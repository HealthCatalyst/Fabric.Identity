using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Xunit;
using IS4 = IdentityServer4.Models;

namespace Fabric.Identity.IntegrationTests
{
    public class ClientRegistrationTests : IntegrationTestsFixture
    {
        private static readonly Random rand = new Random(DateTime.Now.Millisecond);
        private static readonly Func<IS4.Client> GetTestClient = () => new IS4.Client()
        {
            ClientId = rand.Next().ToString(),
            ClientName = "ClientName",
            AllowedScopes = new List<string>() { "scope" },
            AllowedGrantTypes = new List<string>() { IS4.GrantType.AuthorizationCode },
            ClientSecrets = new List<IS4.Secret>() { new IS4.Secret(rand.Next().ToString()) }
        };

        [Fact]
        public async Task TestCreateClient_Success()
        {
            var testClient = GetTestClient();
            var stringContent = new StringContent(JsonConvert.SerializeObject(testClient), Encoding.UTF8, "application/json");
            var response = await this.Client.PostAsync("/api/Client", stringContent);
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);

            var content = await response.Content.ReadAsStringAsync();
            Assert.True(content.Contains(testClient.ClientId));
        }

        [Fact]
        public async Task TestCreateClient_DuplicateIdFailure()
        {
            var testClient = GetTestClient();
            var stringContent = new StringContent(JsonConvert.SerializeObject(testClient), Encoding.UTF8, "application/json");
            var response = await this.Client.PostAsync("/api/Client", stringContent);
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);

            // Send POST with same clientId
            stringContent = new StringContent(JsonConvert.SerializeObject(testClient), Encoding.UTF8, "application/json");
            response = await this.Client.PostAsync("/api/Client", stringContent);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task TestGetClient_Success()
        {
            var testClient = GetTestClient();
            var stringContent = new StringContent(JsonConvert.SerializeObject(testClient), Encoding.UTF8, "application/json");
            var response = await this.Client.PostAsync("/api/Client", stringContent);
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);

            response = await this.Client.SendAsync(new HttpRequestMessage(new HttpMethod("GET"), $"/api/Client/{testClient.ClientId}"));
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var content = await response.Content.ReadAsStringAsync();
            Assert.True(content.Contains(testClient.ClientId));
            Assert.True(!content.Contains("Secret"));
        }

        [Fact]
        public async Task TestDeleteClient_Success()
        {
            var testClient = GetTestClient();
            var stringContent = new StringContent(JsonConvert.SerializeObject(testClient), Encoding.UTF8, "application/json");
            var response = await this.Client.PostAsync("/api/Client", stringContent);
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);

            response = await this.Client.SendAsync(new HttpRequestMessage(new HttpMethod("DELETE"), "/api/Client/{testClient.ClientId}"));
            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

            response = await this.Client.SendAsync(new HttpRequestMessage(new HttpMethod("GET"), "/api/Client/{testClient.ClientId}"));
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }
    }
}
