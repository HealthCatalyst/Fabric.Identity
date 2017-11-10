﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using Fabric.Identity.API;
using Fabric.Identity.API.Configuration;
using Fabric.Identity.API.Persistence;
using Fabric.Identity.API.Persistence.CouchDb.Configuration;
using Fabric.Identity.API.Persistence.CouchDb.Services;
using Fabric.Identity.API.Persistence.InMemory.Services;
using Fabric.Identity.API.Persistence.SqlServer.Configuration;
using Fabric.Identity.API.Persistence.SqlServer.Mappers;
using Fabric.Identity.API.Persistence.SqlServer.Services;
using Fabric.Identity.IntegrationTests.ServiceTests;
using IdentityModel;
using IdentityModel.Client;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Serilog;
using IS4 = IdentityServer4.Models;

namespace Fabric.Identity.IntegrationTests
{
    public class IntegrationTestsFixture : IDisposable
    {
        private const string ClientSecret = "secret";
        private const string IdentityServerUrl = "http://localhost:5001";
        private const string RegistrationApiServerUrl = "http://localhost:5000";
        private const string RegistrationApiName = "registration-api";
        private static readonly IS4.Client Client;
        private static readonly string TokenEndpoint = $"{IdentityServerUrl}/connect/token";
        protected static readonly string TestScope = "testscope";
        protected static readonly string TestClientName = "test-client";
        private static readonly string CouchDbServerEnvironmentVariable = "COUCHDBSETTINGS__SERVER";
        private static readonly string CouchDbUsernameEnvironmentVariable = "COUCHDBSETTINGS__USERNAME";
        private static readonly string CouchDbPasswordEnvironmentVariable = "COUCHDBSETTINGS__PASSWORD";
        private static readonly IDocumentDbService InMemoryDocumentDbService = new InMemoryDocumentService();
        private static ICouchDbSettings _settings;
        private static IAppConfiguration _appConfiguration;
        private static IIdentityDbContext _sqlServerDbContext;
        private static readonly LdapSettings LdapSettings = LdapTestHelper.GetLdapSettings();

        private static IDocumentDbService _dbService;
        private readonly TestServer _apiTestServer;
        private readonly TestServer _identityTestServer;

        static IntegrationTestsFixture()
        {
            var api = GetTestApiResource();
            Client = GetTestClient();
            InMemoryDocumentDbService.AddDocument(api.Name, api);
            InMemoryDocumentDbService.AddDocument(Client.ClientId, Client);
            CouchDbService.AddDocument(api.Name, api);
            CouchDbService.AddDocument(Client.ClientId, Client);
            AddTestEntitiesToSql(Client, api);
        }

        public IntegrationTestsFixture(string storageProvider = FabricIdentityConstants.StorageProviders.InMemory)
        {
            _identityTestServer = CreateIdentityTestServer(storageProvider);
            _apiTestServer = CreateRegistrationApiTestServer(storageProvider);
            HttpClient = GetHttpClient();
        }

        private static IIdentityDbContext SqlServerService
        {
            get
            {
                if (_sqlServerDbContext != null)
                {
                    return _sqlServerDbContext;
                }

                var serviceProvider = new ServiceCollection()
                    .AddEntityFrameworkSqlServer()
                    .BuildServiceProvider();

                var builder = new DbContextOptionsBuilder<IdentityDbContext>();

                builder.UseSqlServer(AppConfiguration.ConnectionStrings.IdentityDatabase)
                    .UseInternalServiceProvider(serviceProvider);

                return _sqlServerDbContext = new IdentityDbContext(builder.Options);
            }
        }

        private static ICouchDbSettings CouchDbSettings => _settings ?? (_settings = new CouchDbSettings
        {
            DatabaseName = "integration-" + DateTime.UtcNow.Ticks,
            Username = "admin",
            Password = "admin",
            Server = "http://127.0.0.1:5984"
        });

        private static IAppConfiguration AppConfiguration => _appConfiguration ?? (_appConfiguration =
                                                                 new AppConfiguration
                                                                 {
                                                                     ConnectionStrings = new ConnectionStrings
                                                                     {
                                                                         IdentityDatabase = $"Server =.;Database = Identity;Trusted_Connection = True;MultipleActiveResultSets = true"
                                                                     }
                                                                 });

        protected static IDocumentDbService CouchDbService
        {
            get
            {
                if (_dbService == null)
                {
                    var couchDbServer = Environment.GetEnvironmentVariable(CouchDbServerEnvironmentVariable);
                    if (!string.IsNullOrEmpty(couchDbServer))
                    {
                        CouchDbSettings.Server = couchDbServer;
                    }
                    var couchDbUsername = Environment.GetEnvironmentVariable(CouchDbUsernameEnvironmentVariable);
                    if (!string.IsNullOrEmpty(couchDbUsername))
                    {
                        CouchDbSettings.Username = couchDbUsername;
                    }
                    var couchDbPassword = Environment.GetEnvironmentVariable(CouchDbPasswordEnvironmentVariable);
                    if (!string.IsNullOrEmpty(couchDbPassword))
                    {
                        CouchDbSettings.Password = couchDbPassword;
                    }

                    var couchDbService = new CouchDbAccessService(CouchDbSettings, new Mock<ILogger>().Object,
                        new SerializationSettings());
                    var bootstrapper =
                        new CouchDbBootstrapper(couchDbService, CouchDbSettings, new Mock<ILogger>().Object);
                    bootstrapper.Setup();

                    _dbService = couchDbService;
                }
                return _dbService;
            }
        }

        public HttpClient HttpClient { get; }

        private TestServer CreateIdentityTestServer(string storageProvider)
        {
            var hostingOptions = new HostingOptions
            {
                UseIis = false,
                UseTestUsers = true,
                StorageProvider = storageProvider
            };

            var builder = new WebHostBuilder();

            builder.ConfigureServices(c =>
                c.AddSingleton(LdapSettings)
                    .AddSingleton(CouchDbSettings)
                    .AddSingleton(hostingOptions)
                    .AddSingleton(AppConfiguration)
            );

            builder.UseKestrel()
                .UseContentRoot(Directory.GetCurrentDirectory())
                .UseStartup<Startup>()
                .UseUrls(IdentityServerUrl);

            return new TestServer(builder);
        }

        private TestServer CreateRegistrationApiTestServer(string storageProvider)
        {
            var options = new IdentityServerAuthenticationOptions
            {
                Authority = IdentityServerUrl,
                ApiName = RegistrationApiName,
                RequireHttpsMetadata = false,
                JwtBackChannelHandler = _identityTestServer.CreateHandler(),
                IntrospectionBackChannelHandler = _identityTestServer.CreateHandler(),
                IntrospectionDiscoveryHandler = _identityTestServer.CreateHandler()
            };

            var hostingOptions = new HostingOptions
            {
                UseIis = false,
                UseTestUsers = true,
                StorageProvider = storageProvider
            };

            var apiBuilder = new WebHostBuilder();

            apiBuilder.ConfigureServices(c => c.AddSingleton(LdapSettings)
                .AddSingleton(options)
                .AddSingleton(CouchDbSettings)
                .AddSingleton(hostingOptions)
                .AddSingleton(AppConfiguration)
            );

            apiBuilder.UseKestrel()
                .UseContentRoot(Directory.GetCurrentDirectory())
                .UseStartup<Startup>()
                .UseUrls(RegistrationApiServerUrl);

            return new TestServer(apiBuilder);
        }

        protected HttpClient GetHttpClient()
        {
            var httpClient = _apiTestServer.CreateClient();
            httpClient.SetBearerToken(GetAccessToken(Client.ClientId, ClientSecret,
                $"{FabricIdentityConstants.IdentityRegistrationScope} {FabricIdentityConstants.IdentityReadScope} {FabricIdentityConstants.IdentitySearchUsersScope}"));
            Console.WriteLine("**********************************Got token from token endpoint");
            return httpClient;
        }

        protected string GetAccessToken(string clientId, string clientSecret, string scope = null)
        {
            var tokenClient =
                new TokenClient(TokenEndpoint, clientId,
                    _identityTestServer.CreateHandler())
                {
                    ClientSecret = clientSecret
                };
            var tokenResponse = tokenClient
                .RequestClientCredentialsAsync(scope).Result;
            if (tokenResponse.IsError)
            {
                throw new InvalidOperationException(tokenResponse.Error);
            }
            return tokenResponse.AccessToken;
        }

        private static IS4.Client GetTestClient()
        {
            return new IS4.Client
            {
                ClientId = TestClientName,
                ClientName = TestClientName,
                ClientSecrets = new List<IS4.Secret> { new IS4.Secret(IS4.HashExtensions.Sha256(ClientSecret)) },
                RequireConsent = false,
                AllowedGrantTypes = IS4.GrantTypes.ClientCredentials,
                AllowedScopes = new List<string>
                {
                    FabricIdentityConstants.IdentityRegistrationScope,
                    FabricIdentityConstants.IdentityReadScope,
                    FabricIdentityConstants.IdentitySearchUsersScope
                }
            };
        }

        private static IS4.ApiResource GetTestApiResource()
        {
            return new IS4.ApiResource
            {
                Name = RegistrationApiName,
                UserClaims = new List<string>
                {
                    JwtClaimTypes.Name,
                    JwtClaimTypes.Email,
                    JwtClaimTypes.Role,
                    FabricIdentityConstants.FabricClaimTypes.Groups
                },
                Scopes = new List<IS4.Scope>
                {
                    new IS4.Scope(FabricIdentityConstants.IdentityRegistrationScope),
                    new IS4.Scope(TestScope),
                    new IS4.Scope(FabricIdentityConstants.IdentityReadScope),
                    new IS4.Scope(FabricIdentityConstants.IdentitySearchUsersScope)
                }
            };
        }

        private static void AddTestEntitiesToSql(IS4.Client client, IS4.ApiResource apiResource)
        {
            var resources = SqlServerService.ApiResources;
            foreach (var apiResourceToDelete in resources)
            {
                SqlServerService.ApiResources.Remove(apiResourceToDelete);
            }
            var clients = SqlServerService.Clients;
            foreach (var clientToDelete in clients)
            {
                SqlServerService.Clients.Remove(clientToDelete);
            }
            SqlServerService.ApiResources.Add(apiResource.ToEntity());
            SqlServerService.Clients.Add(client.ToEntity());
            SqlServerService.SaveChanges();
        }

        #region IDisposable implementation

        // Dispose() calls Dispose(true)
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~IntegrationTestsFixture()
        {
            // Finalizer calls Dispose(false)
            Dispose(false);
        }

        // The bulk of the clean-up code is implemented in Dispose(bool)
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                // free managed resources
                HttpClient.Dispose();
                _identityTestServer.Dispose();
                _apiTestServer.Dispose();
            }
        }

        #endregion IDisposable implementation
    }
}