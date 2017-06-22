using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Fabric.Identity.API;
using Fabric.Identity.API.Services;
using IdentityModel;
using IdentityModel.Client;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using IS4 = IdentityServer4.Models;

namespace Fabric.Identity.IntegrationTests
{
    public class IntegrationTestsFixture : IDisposable
    {
        private readonly TestServer _identityTestServer;
        private readonly TestServer _apiTestServer;
        private static readonly IS4.Client Client;
        private static readonly string ClientSecret = "secret";
        private static readonly string IdentityServerUrl = "http://localhost:5001";
        private static readonly string RegistrationApiServerUrl = "http://localhost:5000";
        private static readonly string TokenEndpoint = $"{IdentityServerUrl}/connect/token";
        private static readonly string RegistrationApiName = "registration-api";
        protected static readonly string TestScope = "testscope";
        private static readonly IDocumentDbService DocumentDbService = new InMemoryDocumentService();

        static IntegrationTestsFixture()
        {
            var api = GetTestApiResource();
            Client = GetTestClient();
            DocumentDbService.AddDocument(api.Name, api);
            DocumentDbService.AddDocument(Client.ClientId, Client);
        }

        public IntegrationTestsFixture()
        {
            _identityTestServer = CreateIdentityTestServer();
            _apiTestServer = CreateRegistrationApiTestServer();
            HttpClient = GetHttpClient();
        }

        private TestServer CreateIdentityTestServer()
        {
            var builder = new WebHostBuilder();
            builder.ConfigureServices(c => c.AddSingleton<IDocumentDbService>(DocumentDbService));

            builder.UseKestrel()
                .UseContentRoot(Directory.GetCurrentDirectory())
                .UseStartup<API.Startup>()
                .UseApplicationInsights()
                .UseUrls(IdentityServerUrl);

            return new TestServer(builder);
        }

        private TestServer CreateRegistrationApiTestServer()
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

            apiBuilder.ConfigureServices(c => c.AddSingleton(options)
                .AddSingleton<IDocumentDbService>(DocumentDbService));

            apiBuilder.UseKestrel()
                .UseContentRoot(Directory.GetCurrentDirectory())
                .UseStartup<API.Startup>()
                .UseApplicationInsights()
                .UseUrls(RegistrationApiServerUrl);

            return new TestServer(apiBuilder);
        }

        protected HttpClient GetHttpClient()
        {
            var httpClient = _apiTestServer.CreateClient();
            httpClient.SetBearerToken(GetAccessToken(Client.ClientId, ClientSecret, FabricIdentityConstants.IdentityRegistrationScope));
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
                ClientId = "test-client",
                ClientName = "Test Client",
                ClientSecrets = new List<IS4.Secret> { new IS4.Secret(IS4.HashExtensions.Sha256(ClientSecret)) },
                RequireConsent = false,
                AllowedGrantTypes = IS4.GrantTypes.ClientCredentials,
                AllowedScopes = new List<string> { FabricIdentityConstants.IdentityRegistrationScope }
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
                Scopes = new List<IS4.Scope> { new IS4.Scope(FabricIdentityConstants.IdentityRegistrationScope), new IS4.Scope(TestScope) }
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