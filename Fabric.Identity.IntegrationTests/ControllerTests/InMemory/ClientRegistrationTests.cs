using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Fabric.Identity.API;
using Newtonsoft.Json;
using Xunit;
using Client = Fabric.Identity.API.Models.Client;
using IS4 = IdentityServer4.Models;

namespace Fabric.Identity.IntegrationTests.ControllerTests.InMemory
{
    public class ClientRegistrationTests : IntegrationTestsFixture
    {
        private static readonly Random Rand = new Random(DateTime.Now.Millisecond);

        public ClientRegistrationTests(string storageProvider = FabricIdentityConstants.StorageProviders.InMemory) : base(storageProvider)
        { }

        private static readonly Func<IS4.Client> GetOnlineTestClient = () => new IS4.Client()
        {
            ClientId = Rand.Next().ToString(),
            ClientName = Rand.Next().ToString(),
            RequireConsent = Rand.Next() % 2 == 0,
            AllowOfflineAccess = false,
            AllowedScopes = new List<string>() { Rand.Next().ToString() },
            AllowedGrantTypes = new List<string>() { IS4.GrantType.Implicit },
            ClientSecrets = new List<IS4.Secret>() { new IS4.Secret(Rand.Next().ToString()) },
            RedirectUris = new List<string>() { Rand.Next().ToString() },
            AllowedCorsOrigins = new List<string>() { Rand.Next().ToString() },
            PostLogoutRedirectUris = new List<string>() { Rand.Next().ToString() },
            
        };

        private static readonly Func<IS4.Client> GetOfflineTestClient = () => new IS4.Client()
        {
            ClientId = Rand.Next().ToString(),
            ClientName = Rand.Next().ToString(),
            RequireConsent = Rand.Next() % 2 == 0,
            AllowOfflineAccess = true,
            AllowedScopes = new List<string>() { Rand.Next().ToString() },
            AllowedGrantTypes = new List<string>() { IS4.GrantType.ClientCredentials },
            ClientSecrets = new List<IS4.Secret>() { new IS4.Secret(Rand.Next().ToString()) },
            RedirectUris = new List<string>() { Rand.Next().ToString() },
            AllowedCorsOrigins = new List<string>() { Rand.Next().ToString() },
            PostLogoutRedirectUris = new List<string>() { Rand.Next().ToString() },
        };

        private static readonly Func<IS4.Client> GetTestClient = Rand.Next() % 2 == 0 ? GetOfflineTestClient : GetOnlineTestClient;

        /// <summary>
        /// A collection of valid clients.
        /// </summary>
        private static IEnumerable<object[]> GetValidClients() => Enumerable.Range(1, 4).Select(_ => new object[] { GetTestClient() });

        private async Task<HttpResponseMessage> CreateNewClient(IS4.Client testClient)
        {
            var stringContent = new StringContent(JsonConvert.SerializeObject(testClient), Encoding.UTF8, "application/json");
            var response = await HttpClient.PostAsync("/api/Client", stringContent);
            return response;
        }

        [Theory]
        [MemberData(nameof(GetValidClients))]
        public async Task TestCreateClient_Success(IS4.Client testClient)
        {
            HttpResponseMessage response = await CreateNewClient(testClient);
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);

            var content = await response.Content.ReadAsStringAsync();
            var client = (Client)JsonConvert.DeserializeObject(content, typeof(Client));

            Assert.Equal(testClient.ClientId, client.ClientId);
            Assert.NotNull(client.ClientSecret);
        }

        [Fact]
        public async Task TestCreateClient_DuplicateIdFailure()
        {
            var testClient = GetTestClient();
            HttpResponseMessage response = await CreateNewClient(testClient);
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);

            // Send POST with same clientId
            response = await CreateNewClient(testClient);
            Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        }

        [Fact]
        public async Task TestGetClient_Success()
        {
            var testClient = GetTestClient();
            HttpResponseMessage response = await CreateNewClient(testClient);
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);

            response = await HttpClient.SendAsync(new HttpRequestMessage(new HttpMethod("GET"), $"/api/Client/{testClient.ClientId}"));
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var content = await response.Content.ReadAsStringAsync();
            var client = (Client)JsonConvert.DeserializeObject(content, typeof(Client));

            Assert.Equal(testClient.ClientId, client.ClientId);
            Assert.Null(client.ClientSecret);
        }

        [Fact]
        public async Task TestGetClient_NotFound()
        {
            var response = await HttpClient.SendAsync(new HttpRequestMessage(new HttpMethod("GET"), $"/api/Client/client-that-does-not-exist"));
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task TestResetPassword_Success()
        {
            var testClient = GetTestClient();
            HttpResponseMessage response = await CreateNewClient(testClient);
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);

            var content = await response.Content.ReadAsStringAsync();
            var client = (Client)JsonConvert.DeserializeObject(content, typeof(Client));

            var clientId = client.ClientId;
            var password = client.ClientSecret;

            response = await HttpClient.SendAsync(new HttpRequestMessage(new HttpMethod("POST"), $"/api/Client/{testClient.ClientId}/resetPassword"));
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            content = await response.Content.ReadAsStringAsync();
            client = (Client)JsonConvert.DeserializeObject(content, typeof(Client));

            Assert.Equal(clientId, client.ClientId);
            Assert.NotEqual(password, client.ClientSecret);
        }

        [Fact]
        public async Task TestDeleteClient_Success()
        {
            var testClient = GetTestClient();
            HttpResponseMessage response = await CreateNewClient(testClient);
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);

            response = await HttpClient.SendAsync(new HttpRequestMessage(new HttpMethod("DELETE"), $"/api/Client/{testClient.ClientId}"));
            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

            // Confirm it's deleted.
            response = await HttpClient.SendAsync(new HttpRequestMessage(new HttpMethod("GET"), $"/api/Client/{testClient.ClientId}"));
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

            // Add the same client again
            response = await CreateNewClient(testClient);
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        }

        [Fact]
        public async Task TestDeleteClient_NotFound()
        {
            var response = await HttpClient.SendAsync(new HttpRequestMessage(new HttpMethod("DELETE"), $"/api/Client/client-that-does-not-exist"));
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task TestUpdateClient_Success()
        {
            var testClient = GetTestClient();
            HttpResponseMessage response = await CreateNewClient(testClient);
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);

            // Update it
            var updatedTestClient = GetTestClient();
            updatedTestClient.ClientId = testClient.ClientId;

            var stringContent = new StringContent(JsonConvert.SerializeObject(updatedTestClient), Encoding.UTF8, "application/json");
            response = await HttpClient.PutAsync($"/api/Client/{testClient.ClientId}", stringContent);
            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

            // Fetch it => confirm it's persisted
            response = await HttpClient.SendAsync(new HttpRequestMessage(new HttpMethod("GET"), $"/api/Client/{testClient.ClientId}"));
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var content = await response.Content.ReadAsStringAsync();
            var getClient = (Client)JsonConvert.DeserializeObject(content, typeof(Client));

            // Must not return password
            Assert.Equal(null, getClient.ClientSecret);
            // Must ignore ClientId in payload
            Assert.Equal(testClient.ClientId, getClient.ClientId);

            Assert.Equal(updatedTestClient.ClientName, getClient.ClientName);
            Assert.Equal(updatedTestClient.AllowedScopes, getClient.AllowedScopes);
            Assert.Equal(updatedTestClient.RedirectUris, getClient.RedirectUris);
            Assert.Equal(updatedTestClient.RequireConsent, getClient.RequireConsent);
            Assert.Equal(updatedTestClient.AllowedCorsOrigins, getClient.AllowedCorsOrigins);
            Assert.Equal(updatedTestClient.AllowOfflineAccess, getClient.AllowOfflineAccess);
            Assert.Equal(updatedTestClient.AllowedGrantTypes, getClient.AllowedGrantTypes);
            Assert.Equal(updatedTestClient.PostLogoutRedirectUris, getClient.PostLogoutRedirectUris);
        }

        [Fact]
        public async Task TestAuthorization_NoToken_Fails()
        {
            var testClient = GetTestClient();
            HttpClient.DefaultRequestHeaders.Remove("Authorization");
            var response = await CreateNewClient(testClient).ConfigureAwait(false);
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task TestAuthorization_NoScope_Fails()
        {
            var testClient = GetOfflineTestClient();
            testClient.AllowedScopes = new List<string>{TestScope};
            var response = await CreateNewClient(testClient);
            response.EnsureSuccessStatusCode();
            var client = JsonConvert.DeserializeObject<Client>(await response.Content.ReadAsStringAsync());
            HttpClient.SetBearerToken(GetAccessToken(client.ClientId, client.ClientSecret, TestScope));
            testClient = GetTestClient();
            response = await CreateNewClient(testClient);
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }    
    }
}