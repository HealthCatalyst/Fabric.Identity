using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Fabric.Identity.API;
using Fabric.Identity.API.Models;
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
        private static Client _client;
        private static readonly object Lock = new object();
        private static readonly string IdentityServerUrl = "http://localhost:5001";
        private static readonly string RegistrationApiServerUrl = "http://localhost:5000";
        private static readonly string TokenEndpoint = $"{IdentityServerUrl}/connect/token";
        private static readonly string RegistrationApiName = "registration-api";
        protected static readonly string TestScope = "testscope";

        public IntegrationTestsFixture()
        {
            _identityTestServer = CreateIdentityTestServer();
            _apiTestServer = CreateRegistrationApiTestServer();
            HttpClient = GetHttpClient();
        }

        private TestServer CreateIdentityTestServer()
        {
            var builder = new WebHostBuilder()
                .UseKestrel()
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
            apiBuilder.ConfigureServices(c => c.AddSingleton(options));

            apiBuilder.UseKestrel()
                .UseContentRoot(Directory.GetCurrentDirectory())
                .UseStartup<API.Startup>()
                .UseApplicationInsights()
                .UseUrls(RegistrationApiServerUrl);

            return new TestServer(apiBuilder);
        }

        private HttpClient GetHttpClient()
        {
            lock (Lock)
            {
                var httpClient = _apiTestServer.CreateClient();
                httpClient.BaseAddress = new Uri(RegistrationApiServerUrl);

                if (_client == null)
                {
                    var createdApiResponse = SetupRegistrationApi(httpClient);
                    createdApiResponse.Result.EnsureSuccessStatusCode();

                    var createdClientResponse = SetupTestClient(httpClient);
                    var result = createdClientResponse.Result;
                    result.EnsureSuccessStatusCode();
                    _client = JsonConvert.DeserializeObject<Client>(result.Content.ReadAsStringAsync().Result);
                }
                httpClient.SetBearerToken(GetAccessToken(_client, FabricIdentityConstants.IdentityRegistrationScope).Result);
                return httpClient;
            }
        }

        protected async Task<string> GetAccessToken(Client client, string scope = null)
        {
            var tokenClient =
                new TokenClient(TokenEndpoint, client.ClientId,
                    _identityTestServer.CreateHandler())
                {
                    ClientSecret = client.ClientSecret
                };
            var tokenResponse = await tokenClient
                .RequestClientCredentialsAsync(scope);
            if (tokenResponse.IsError)
            {
                throw new Exception(tokenResponse.Error);
            }
            return tokenResponse.AccessToken;
        }

        private async Task<HttpResponseMessage> SetupRegistrationApi(HttpClient httpClient)
        {
            var response = await httpClient.PostAsync("api/apiResource", GetStringContent(GetTestApiResource()));
            return response;
        }

        private async Task<HttpResponseMessage> SetupTestClient(HttpClient httpClient)
        {
            var response = await httpClient.PostAsync("api/client", GetStringContent(GetTestClient()));
            return response;
        }

        private IS4.Client GetTestClient()
        {
            return new IS4.Client
            {
                ClientId = "test-client",
                ClientName = "Test Client",
                RequireConsent = false,
                AllowedGrantTypes = IS4.GrantTypes.ClientCredentials,
                AllowedScopes = new List<string> { FabricIdentityConstants.IdentityRegistrationScope }
            };
        }

        private StringContent GetStringContent(object entity)
        {
            return new StringContent(JsonConvert.SerializeObject(entity), Encoding.UTF8, "application/json");
        }

        private IS4.ApiResource GetTestApiResource()
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
                Scopes = new List<IS4.Scope> {new IS4.Scope(FabricIdentityConstants.IdentityRegistrationScope), new IS4.Scope(TestScope)}
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