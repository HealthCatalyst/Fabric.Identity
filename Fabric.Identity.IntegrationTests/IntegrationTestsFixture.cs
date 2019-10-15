using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Net.Http;
using System.Security.Claims;
using System.Threading.Tasks;
using Fabric.Identity.API;
using Fabric.Identity.API.Configuration;
using Fabric.Identity.API.Logging;
using Fabric.Identity.API.Persistence;
using Fabric.Identity.API.Persistence.CouchDb.Configuration;
using Fabric.Identity.API.Persistence.CouchDb.Services;
using Fabric.Identity.API.Persistence.InMemory.Services;
using Fabric.Identity.API.Persistence.SqlServer.Configuration;
using Fabric.Identity.API.Persistence.SqlServer.Mappers;
using Fabric.Identity.API.Persistence.SqlServer.Services;
using Fabric.Identity.API.Services;
using Fabric.Identity.IntegrationTests.ServiceTests;
using Fabric.Platform.Shared.Configuration.Docker;
using IdentityModel;
using IdentityModel.Client;
using IdentityServer4.AccessTokenValidation;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Serilog;
using IS4 = IdentityServer4.Models;

namespace Fabric.Identity.IntegrationTests
{
    public class IntegrationTestsFixture : IDisposable
    {
        private const string ClientSecret = "secret";
        protected const string IdentityServerUrl = "http://localhost:5001";
        private const string RegistrationApiServerUrl = "http://localhost:5000";
        private const string RegistrationApiName = "registration-api";
        private static readonly IS4.Client Client;
        protected static readonly string TokenEndpoint = $"{IdentityServerUrl}/connect/token";
        protected static readonly string TestScope = "testscope";
        protected static readonly string TestClientName = "test-client";
        private static readonly long DatabaseNameSuffix = DateTime.UtcNow.Ticks;
        private static readonly string SqlServerEnvironmentVariable = "SQLSERVERSETTINGS__SERVER";
        private static readonly string SqlServerUsernameEnvironmentVariable = "SQLSERVERSETTINGS__USERNAME";
        private static readonly string SqlServerPassworEnvironmentVariable = "SQLSERVERSETTINGS__PASSWORD";
        private static readonly string CouchDbServerEnvironmentVariable = "COUCHDBSETTINGS__SERVER";
        private static readonly string CouchDbUsernameEnvironmentVariable = "COUCHDBSETTINGS__USERNAME";
        private static readonly string CouchDbPasswordEnvironmentVariable = "COUCHDBSETTINGS__PASSWORD";
        private static readonly IDocumentDbService InMemoryDocumentDbService = new InMemoryDocumentService();
        private static ICouchDbSettings _settings;
        private static IConnectionStrings _connectionStrings;
        
        private static readonly LdapSettings LdapSettings = LdapTestHelper.GetLdapSettings();

        private static IDocumentDbService _dbService;
        private readonly TestServer _apiTestServer;
        protected readonly TestServer IdentityTestServer;

        static IntegrationTestsFixture()
        {
            var api = GetTestApiResource();
            Client = GetTestClient();
            InMemoryDocumentDbService.AddDocument(api.Name, api);
            InMemoryDocumentDbService.AddDocument(Client.ClientId, Client);
            CreateSqlServerDatabase();
            AddTestEntitiesToSql(Client, api);
        }



        public IntegrationTestsFixture(string storageProvider = FabricIdentityConstants.StorageProviders.InMemory)
        {
            IdentityTestServer = CreateIdentityTestServer(storageProvider);
            _apiTestServer = CreateRegistrationApiTestServer(storageProvider);
            _httpClientTaskCompletionSource = new TaskCompletionSource<HttpClient>();
            _ = SetupHttpClient();
        }

        private static ICouchDbSettings CouchDbSettings => _settings ?? (_settings = new CouchDbSettings
        {
            DatabaseName = $"integration-{DatabaseNameSuffix}",
            Username = "",
            Password = "",
            Server = "http://127.0.0.1:5984"
        });

        private static string SqlServerHost => Environment.GetEnvironmentVariable(SqlServerEnvironmentVariable) ?? ".";

        private static string SqlServerSecurityString
        {
            get
            {
                var sqlServerUserName = Environment.GetEnvironmentVariable(SqlServerUsernameEnvironmentVariable);
                var sqlServerPassword = Environment.GetEnvironmentVariable(SqlServerPassworEnvironmentVariable);
                var securityString = "Trusted_Connection=True";
                if (!string.IsNullOrEmpty(sqlServerUserName) && !string.IsNullOrEmpty(sqlServerPassword))
                {
                    securityString = $"User Id={sqlServerUserName};Password={sqlServerPassword}";
                }
                return securityString;
            }
        }

        private static IConnectionStrings ConnectionStrings
        {
            get
            {
                if (_connectionStrings != null) return _connectionStrings;
                _connectionStrings = new ConnectionStrings
                {
                    IdentityDatabase =
                        $"Server={SqlServerHost};Database=Identity-{DatabaseNameSuffix};{SqlServerSecurityString};MultipleActiveResultSets=true"
                };
                Console.WriteLine($"Connection String for tests: {_connectionStrings.IdentityDatabase}");
                return _connectionStrings;
            }
        }

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

        protected static IdentityDbContext IdentityDbContext
        {
            get
            {
                var serviceProvider = new ServiceCollection()
                    .AddEntityFrameworkSqlServer()
                    .BuildServiceProvider();

                var builder = new DbContextOptionsBuilder<IdentityDbContext>();

                builder.UseSqlServer(ConnectionStrings.IdentityDatabase)
                    .UseInternalServiceProvider(serviceProvider);

                var testIdentity = new ClaimsIdentity();
                testIdentity.AddClaim(new Claim(JwtClaimTypes.ClientId, "testing"));

                var contextAccessor = new HttpContextAccessor()
                {
                    HttpContext = new DefaultHttpContext
                    {
                        User = new ClaimsPrincipal(testIdentity)
                    }
                };

                return new IdentityDbContext(builder.Options, new UserResolverService(contextAccessor));
            }
        }

        private readonly TaskCompletionSource<HttpClient> _httpClientTaskCompletionSource;

        public Task<HttpClient> HttpClient => _httpClientTaskCompletionSource.Task;

        private TestServer CreateIdentityTestServer(string storageProvider)
        {
            var loggerConfiguration = new LoggerConfiguration();

            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json")
                .AddEnvironmentVariables()
                .AddDockerSecrets(typeof(IAppConfiguration))
                .SetBasePath(Directory.GetCurrentDirectory())
                .Build();

            var certificateService = IdentityConfigurationProvider.MakeCertificateService();
            var decryptionService = new DecryptionService(certificateService);
            var appConfig = new IdentityConfigurationProvider(configuration).GetAppConfiguration(decryptionService);

            LogFactory.ConfigureTraceLogger(loggerConfiguration, appConfig.ApplicationInsights);

            Log.Logger = loggerConfiguration.CreateLogger();

            var hostingOptions = new HostingOptions
            {
                UseIis = false,
                UseTestUsers = true,
                StorageProvider = storageProvider
            };

            var builder = WebHost.CreateDefaultBuilder();

            builder.ConfigureServices(c =>
                c.AddSingleton(LdapSettings)
                    .AddSingleton(Log.Logger)
                    .AddSingleton(CouchDbSettings)
                    .AddSingleton(hostingOptions)
                    .AddSingleton(ConnectionStrings));

            builder.ConfigureAppConfiguration((hostContext, config) =>
                {
                    config.AddDockerSecrets(typeof(IAppConfiguration));
                    config.SetBasePath(Directory.GetCurrentDirectory());
                })
                .UseUrls(IdentityServerUrl)
                .ConfigureKestrel((context, options) =>
                {

                })
                .UseSerilog()
                .UseStartup<Startup>();

            return new TestServer(builder);
        }

        private TestServer CreateRegistrationApiTestServer(string storageProvider)
        {
            var loggerConfiguration = new LoggerConfiguration();

            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json")
                .AddEnvironmentVariables()
                .AddDockerSecrets(typeof(IAppConfiguration))
                .SetBasePath(Directory.GetCurrentDirectory())
                .Build();

            var certificateService = IdentityConfigurationProvider.MakeCertificateService();
            var decryptionService = new DecryptionService(certificateService);
            var appConfig = new IdentityConfigurationProvider(configuration).GetAppConfiguration(decryptionService);

            LogFactory.ConfigureTraceLogger(loggerConfiguration, appConfig.ApplicationInsights);

            Log.Logger = loggerConfiguration.CreateLogger();

            // I don't think we use this anymore
            var options = new IdentityServerAuthenticationOptions
            {
                Authority = IdentityServerUrl,
                ApiName = RegistrationApiName,
                RequireHttpsMetadata = false,
                JwtBackChannelHandler = IdentityTestServer.CreateHandler(),
                IntrospectionBackChannelHandler = IdentityTestServer.CreateHandler(),
                IntrospectionDiscoveryHandler = IdentityTestServer.CreateHandler()
            };

            var hostingOptions = new HostingOptions
            {
                UseIis = false,
                UseTestUsers = true,
                StorageProvider = storageProvider
            };

            var apiBuilder = WebHost.CreateDefaultBuilder();

            apiBuilder.ConfigureServices(c => c.AddSingleton(LdapSettings)
                .AddSingleton(options)
                .AddSingleton(Log.Logger)
                .AddSingleton(CouchDbSettings)
                .AddSingleton(hostingOptions)
                .AddSingleton(ConnectionStrings)
            );

            apiBuilder.ConfigureServices((builder, services) =>
            {
                services.Configure<JwtBearerOptions>("Bearer", jwtOpts =>
                {
                    jwtOpts.BackchannelHttpHandler = IdentityTestServer.CreateHandler();
                });
            });

            apiBuilder
                .ConfigureAppConfiguration((hostContext, config) =>
                {
                    config.AddDockerSecrets(typeof(IAppConfiguration));
                    config.SetBasePath(Directory.GetCurrentDirectory());
                })
                .UseUrls(RegistrationApiServerUrl)
                .ConfigureKestrel((context, kestrelOptions) =>
                {

                })
                .UseSerilog()
                .UseStartup<Startup>();

            return new TestServer(apiBuilder);
        }

        protected async Task SetupHttpClient()
        {
            var httpClient = _apiTestServer.CreateClient();
            httpClient.SetBearerToken(await GetAccessToken(Client.ClientId, ClientSecret,
                $"{FabricIdentityConstants.IdentityRegistrationScope} {FabricIdentityConstants.IdentityReadScope} {FabricIdentityConstants.IdentitySearchUsersScope}"));
            Console.WriteLine("**********************************Got token from token endpoint");
            _httpClientTaskCompletionSource.SetResult(httpClient);
        }

        protected async Task<string> GetAccessToken(string clientId, string clientSecret, string scope = null)
        {
            var tokenRequest = new ClientCredentialsTokenRequest
                {
                    Address = TokenEndpoint,
                    ClientId = clientId,
                    ClientSecret = clientSecret,
                    Scope = scope
                };

            var httpClient = new HttpClient(IdentityTestServer.CreateHandler());
            var tokenResponse = await httpClient.RequestClientCredentialsTokenAsync(tokenRequest);
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

        private static void CreateSqlServerDatabase()
        {
            var connection =
                $"Data Source={SqlServerHost};Initial Catalog=master;{SqlServerSecurityString};MultipleActiveResultSets=True";
            var file = new FileInfo("Fabric.Identity.SqlServer_Create.sql");

            var dbName = $"Identity-{DatabaseNameSuffix}";
            var createDbScript = file.OpenText().ReadToEnd()
                .Replace("$(DatabaseName)", dbName);

            var splitter = new[] { "GO\r\n" };
            var commandTexts = createDbScript.Split(splitter, StringSplitOptions.RemoveEmptyEntries);

            int x;
            using (var conn = new SqlConnection(connection))
            {
                conn.Open();
                using (var command = new SqlCommand("query", conn))
                {
                    for (x = 0; x < commandTexts.Length; x++)
                    {
                        var commandText = commandTexts[x];

                        // break if we just created the Identity DB
                        if (commandText.StartsWith("CREATE DATABASE"))
                        {
                            var commandParts = commandText.Split(
                                new[] { " ON " },
                                StringSplitOptions.RemoveEmptyEntries);

                            command.CommandText = commandParts[0];
                            command.ExecuteNonQuery();
                            break;
                        }
                    }
                }
            }

            // establish a connection to the newly created Identity DB
            using (var conn = new SqlConnection(ConnectionStrings.IdentityDatabase))
            {
                conn.Open();

                using (var command = new SqlCommand("query", conn))
                {
                    for (x = x + 1; x < commandTexts.Length; x++)
                    {
                        var commandText = commandTexts[x];

                        // skip generated SqlPackage commands and comments
                        if (commandText.StartsWith(":") || commandText.StartsWith("/*"))
                        {
                            continue;
                        }

                        if (commandText.StartsWith("CREATE TABLE"))
                        {
                            var commandParts = commandText.Split(
                                new[] { " ON " },
                                StringSplitOptions.RemoveEmptyEntries);

                            command.CommandText = commandParts[0];
                            command.ExecuteNonQuery();
                        }
                        else if (commandText.StartsWith("CREATE NONCLUSTERED INDEX") || commandText.StartsWith("CREATE UNIQUE NONCLUSTERED INDEX"))
                        {
                            var commandParts = commandText.Split(
                                new[] { " ON " },
                                StringSplitOptions.RemoveEmptyEntries);

                            command.CommandText = commandParts[0] + "ON " + commandParts[1];
                            command.ExecuteNonQuery();
                        }

                        command.CommandText = commandText.TrimEnd(Environment.NewLine.ToCharArray());

                        try
                        {
                            command.ExecuteNonQuery();
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex);
                        }
                    }
                }
            }
        }

        private static void AddTestEntitiesToSql(IS4.Client client, IS4.ApiResource apiResource)
        {
            using (var identityContext = IdentityDbContext)
            {
                identityContext.Database.ExecuteSqlCommand(DeleteDataSql);
                identityContext.ApiResources.Add(apiResource.ToEntity());
                identityContext.Clients.Add(client.ToEntity());
                identityContext.SaveChanges();
            }
        }

        private static readonly string DeleteDataSql =
            @"DELETE FROM ApiResources; 
              DELETE FROM Clients;
              DELETE FROM IdentityResources;
              DELETE FROM UserLogins;
              DELETE FROM UserClaims;
              DELETE FROM Users";

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
                HttpClient.Wait();
                HttpClient.Result.Dispose();
                
                IdentityTestServer.Dispose();
                _apiTestServer.Dispose();
            }
        }

        #endregion IDisposable implementation
    }
}