using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using Fabric.Identity.API;
using Fabric.Identity.API.Configuration;
using Fabric.Identity.API.CouchDb;
using Fabric.Identity.API.Services;
using IdentityModel;
using IdentityModel.Client;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Serilog;
using IS4 = IdentityServer4.Models;

namespace Fabric.Identity.IntegrationTests
{
    public class IntegrationTestsFixture : IDisposable
    {
        private readonly TestServer _identityTestServer;
        private readonly TestServer _apiTestServer;
        private static IS4.Client Client;
        private static readonly string ClientSecret = "secret";
        private static readonly string IdentityServerUrl = "http://localhost:5001";
        private static readonly string RegistrationApiServerUrl = "http://localhost:5000";
        private static readonly string TokenEndpoint = $"{IdentityServerUrl}/connect/token";
        private static readonly string RegistrationApiName = "registration-api";
        protected static readonly string TestScope = "testscope";
        protected static readonly string TestClientName = "test-client";
        private static readonly string CouchDbServerEnvironmentVariable = "COUCHDBSETTINGS__SERVER";
        private static readonly string CouchDbUsernameEnvironmentVariable = "COUCHDBSETTINGS__USERNAME";
        private static readonly string CouchDbPasswordEnvironmentVariable = "COUCHDBSETTINGS__PASSWORD";

        private static readonly IDocumentDbService InMemoryDocumentDbService = new InMemoryDocumentService();
        private static ICouchDbSettings _settings;
        private static readonly LdapSettings LdapSettings = LdapTestHelper.GetLdapSettings();
        private static ICouchDbSettings CouchDbSettings => _settings ?? (_settings = new CouchDbSettings()
        {
            DatabaseName = "integration-" + DateTime.UtcNow.Ticks,
            Username = "",
            Password = "",
            Server = "http://127.0.0.1:5984"
        });
        

        private static IDocumentDbService _dbService;
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

                    var api = GetTestApiResource();
                    Client = GetTestClient();
                    couchDbService.AddDocument(api.Name, api);
                    couchDbService.AddDocument(Client.ClientId, Client);

                    _dbService = couchDbService;
                }
                return _dbService;
            }
        }

        static IntegrationTestsFixture()
        {                
            var api = GetTestApiResource();
            Client = GetTestClient();
            InMemoryDocumentDbService.AddDocument(api.Name, api);
            InMemoryDocumentDbService.AddDocument(Client.ClientId, Client);
        }

        public IntegrationTestsFixture(bool useInMemoryDbService = true)
        {
            var dbService = GetDocumentDbService(useInMemoryDbService);    

            _identityTestServer = CreateIdentityTestServer(dbService);
            _apiTestServer = CreateRegistrationApiTestServer(dbService);
            HttpClient = GetHttpClient();
        }

        private IDocumentDbService GetDocumentDbService(bool useInMemoryDbService)
        {
            return useInMemoryDbService ? InMemoryDocumentDbService : CouchDbService;
        }

        private TestServer CreateIdentityTestServer(IDocumentDbService documentDbService)
        {
            var builder = new WebHostBuilder();
            builder.ConfigureServices(c =>
                c.AddSingleton(LdapSettings)
                .AddSingleton(CouchDbSettings)
                .AddSingleton<IDocumentDbService>(documentDbService)
            );
            

            builder.UseKestrel()
                .UseContentRoot(Directory.GetCurrentDirectory())
                .UseStartup<API.Startup>()                
                .UseUrls(IdentityServerUrl);

            return new TestServer(builder);
        }

        private TestServer CreateRegistrationApiTestServer(IDocumentDbService documentDbService)
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

            var apiBuilder = new WebHostBuilder();

            apiBuilder.ConfigureServices(c => c.AddSingleton(LdapSettings)
                .AddSingleton(options)
                .AddSingleton(CouchDbSettings)
                .AddSingleton<IDocumentDbService>(documentDbService)
                );

            apiBuilder.UseKestrel()
                .UseContentRoot(Directory.GetCurrentDirectory())
                .UseStartup<API.Startup>()                
                .UseUrls(RegistrationApiServerUrl);

            return new TestServer(apiBuilder);
        }

        protected HttpClient GetHttpClient()
        {
            var httpClient = _apiTestServer.CreateClient();
            httpClient.SetBearerToken(GetAccessToken(Client.ClientId, ClientSecret, $"{FabricIdentityConstants.IdentityRegistrationScope} {FabricIdentityConstants.IdentityReadScope}"));
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
                AllowedScopes = new List<string> { FabricIdentityConstants.IdentityRegistrationScope, FabricIdentityConstants.IdentityReadScope }
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
                    new IS4.Scope(FabricIdentityConstants.IdentityReadScope)
                }
            };
        }

        public HttpClient HttpClient { get; }

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
                this.HttpClient.Dispose();
                _identityTestServer.Dispose();
                _apiTestServer.Dispose();
            }
        }

        #endregion IDisposable implementation
    }
}