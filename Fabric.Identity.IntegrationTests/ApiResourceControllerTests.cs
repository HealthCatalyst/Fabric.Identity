﻿using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Fabric.Identity.API;
using Fabric.Identity.API.Models;
using Newtonsoft.Json;
using Xunit;
using IS4 = IdentityServer4.Models;

namespace Fabric.Identity.IntegrationTests
{
    public class ApiResourceControllerTests : IntegrationTestsFixture
    {
        private static readonly Random Rand = new Random(DateTime.Now.Millisecond);

        public ApiResourceControllerTests(string provider = FabricIdentityConstants.StorageProviders.InMemory) : base(provider)
        { }

        private static readonly Func<IS4.ApiResource> GetTestApiResource = () => new IS4.ApiResource
        {
            Name = Rand.Next().ToString(),
            DisplayName = "ApiResourceName",
            Scopes = new List<IS4.Scope> {new IS4.Scope(Rand.Next().ToString())},
            Enabled = true,
            UserClaims = new List<string> {Rand.Next().ToString()}
        };

        private async Task<HttpResponseMessage> CreateNewResource(IS4.ApiResource testApiResource)
        {
            var stringContent = new StringContent(JsonConvert.SerializeObject(testApiResource), Encoding.UTF8,
                "application/json");
            var response = await HttpClient.PostAsync("/api/ApiResource", stringContent);
            return response;
        }
        
        [Fact]
        public async Task TestCreateApiResource_DuplicateIdFailure()
        {
            var testApiResource = GetTestApiResource();
            Console.WriteLine("calling create for test client 1");
            var response = await CreateNewResource(testApiResource);
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);

            // Send POST with same Name
            Console.WriteLine("calling create for test client 2");
            response = await CreateNewResource(testApiResource);
            Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        }

        [Fact]
        public async Task TestCreateApiResource_Success()
        {
            var testApiResource = GetTestApiResource();
            var response = await CreateNewResource(testApiResource);
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);

            var content = await response.Content.ReadAsStringAsync();
            var resource = (ApiResource) JsonConvert.DeserializeObject(content, typeof(ApiResource));

            Assert.Equal(testApiResource.Name, resource.Name);
            Assert.NotNull(resource.ApiSecret);
        }

        [Fact]
        public async Task TestGetApiResource_Success()
        {
            var testApiResource = GetTestApiResource();
            var response = await CreateNewResource(testApiResource);
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);

            response = await HttpClient.SendAsync(
                new HttpRequestMessage(new HttpMethod("GET"), $"/api/ApiResource/{testApiResource.Name}"));
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var content = await response.Content.ReadAsStringAsync();
            var resource = (ApiResource) JsonConvert.DeserializeObject(content, typeof(ApiResource));

            Assert.Equal(testApiResource.Name, resource.Name);
            Assert.Null(resource.ApiSecret);
        }

        [Fact]
        public async Task TestResetPassword_Success()
        {
            var testApiResource = GetTestApiResource();
            var response = await CreateNewResource(testApiResource);
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);

            var content = await response.Content.ReadAsStringAsync();
            var resource = (ApiResource) JsonConvert.DeserializeObject(content, typeof(ApiResource));

            var name = resource.Name;
            var password = resource.ApiSecret;

            response = await HttpClient.SendAsync(new HttpRequestMessage(new HttpMethod("GET"),
                $"/api/ApiResource/{testApiResource.Name}/resetPassword"));
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            content = await response.Content.ReadAsStringAsync();
            resource = (ApiResource) JsonConvert.DeserializeObject(content, typeof(ApiResource));

            Assert.Equal(name, resource.Name);
            Assert.NotEqual(password, resource.ApiSecret);
        }

        [Fact]
        public async Task TestDeleteApiResource_Success()
        {
            var testApiResource = GetTestApiResource();
            var response = await CreateNewResource(testApiResource);
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);

            // Send POST with same Name
            Console.WriteLine("calling create for test client 2");
            response = await CreateNewResource(testApiResource);
            Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);

            response = await HttpClient.SendAsync(new HttpRequestMessage(new HttpMethod("DELETE"),
                $"/api/ApiResource/{testApiResource.Name}"));
            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

            response = await HttpClient.SendAsync(
                new HttpRequestMessage(new HttpMethod("GET"), $"/api/ApiResource/{testApiResource.Name}"));
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task TestDeleteApiResource_NotFound()
        {
            var response = await HttpClient.SendAsync(new HttpRequestMessage(new HttpMethod("DELETE"), $"/api/ApiResource/resource-that-does-not-exist"));
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }
    }
}