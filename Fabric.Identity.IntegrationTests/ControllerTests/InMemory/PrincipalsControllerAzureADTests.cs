using Fabric.Identity.API;
using Fabric.Identity.API.Configuration;
using Fabric.Identity.API.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Fabric.Identity.IntegrationTests.ControllerTests.InMemory
{
    public class PrincipalsControllerAzureADTests : IntegrationTestsFixture
    {
        private readonly string _principalsBaseUrl = "/api/principals";
        private readonly AppConfiguration _appConfig;

        public PrincipalsControllerAzureADTests(string provider = FabricIdentityConstants.StorageProviders.InMemory) : base(provider)
        {
            _appConfig = new AppConfiguration
            {
                DomainName = "testingAzure"
            };
        }

        [Fact]
        public async Task SearchPrincipals_FindGroups_Succeeds_Async()
        {
            var searchQuery = "searchtext=ITGroup";
            var typeQuery = "type=group";
            var enpoint = $"{_principalsBaseUrl}/AzureActiveDirectory/search?{searchQuery}&{typeQuery}";

            var httpClient = await HttpClient;
            var searchResult = await httpClient.SendAsync(new HttpRequestMessage(new HttpMethod("GET"), enpoint));

            Assert.Equal(HttpStatusCode.OK, searchResult.StatusCode);

            var groupsJson = JObject.Parse(await searchResult.Content.ReadAsStringAsync());
            var groups = groupsJson.ToObject<SearchResultApiModel<FabricPrincipalApiModel>>();

            Assert.Equal(4, groups.ResultCount);
            Assert.Equal(4, groups.Principals.Select(p => p.IdentityProvider.Equals("Azure")).Count());
            Assert.Equal(4, groups.Principals.Select(p => p.PrincipalType.Equals("group")).Count());
        }

        [Fact]
        public async Task SearchPrincipals_FindGroupsWithTenant_Succeeds_Async()
        {
            var searchQuery = "searchtext=ITGroup";
            var typeQuery = "type=group";
            var tenantQuery = "tenantid=1";
            var enpoint = $"{_principalsBaseUrl}/AzureActiveDirectory/search?{searchQuery}&{typeQuery}&{tenantQuery}";

            var httpClient = await HttpClient;
            var searchResult = await httpClient.SendAsync(new HttpRequestMessage(new HttpMethod("GET"), enpoint));

            Assert.Equal(HttpStatusCode.OK, searchResult.StatusCode);

            var groupsJson = JObject.Parse(await searchResult.Content.ReadAsStringAsync());
            var groups = groupsJson.ToObject<SearchResultApiModel<FabricPrincipalApiModel>>();

            Assert.Equal(4, groups.ResultCount);
            Assert.Equal(4, groups.Principals.Select(p => p.IdentityProvider.Equals("Azure")).Count());
            Assert.Equal(4, groups.Principals.Select(p => p.PrincipalType.Equals("group")).Count());
            Assert.Equal(4, groups.Principals.Select(p => p.TenantId.Equals("TenantId")).Count());
        }

        [Fact]
        public async Task SearchPrincipals_FindAzureUsersNoTenant_Succeeds_Async()
        {
            var searchQuery = "searchtext=johnny";
            var typeQuery = "type=user";
            var enpoint = $"{_principalsBaseUrl}/AzureActiveDirectory/search?{searchQuery}&{typeQuery}";

            var httpClient = await HttpClient;
            var searchResult = await httpClient.SendAsync(new HttpRequestMessage(new HttpMethod("GET"), enpoint));

            Assert.Equal(HttpStatusCode.OK, searchResult.StatusCode);

            var usersJson = JObject.Parse(await searchResult.Content.ReadAsStringAsync());
            var users = usersJson.ToObject<SearchResultApiModel<FabricPrincipalApiModel>>();

            Assert.Equal(3, users.ResultCount);
            Assert.Equal(3, users.Principals.Select(p => p.IdentityProvider.Equals("Azure")).Count());
            Assert.Equal(3, users.Principals.Select(p => p.PrincipalType.Equals("user")).Count());
            Assert.Equal(3, users.Principals.Select(p => p.TenantId.Equals("TenantId")).Count());
            Assert.Equal(3, users.Principals.Select(p => p.IdentityProviderUserPrincipalName.Equals("UserPrincipal")).Count());
        }

        [Fact]
        public async Task SearchPrincipals_FindAzureUser_Succeeds_Async()
        {
            var searchQuery = "searchtext=johnny";
            var typeQuery = "type=user";
            var tenantQuery = "tenantid=2";
            var enpoint = $"{_principalsBaseUrl}/AzureActiveDirectory/search?{searchQuery}&{typeQuery}&{tenantQuery}";

            var httpClient = await HttpClient;
            var searchResult = await httpClient.SendAsync(new HttpRequestMessage(new HttpMethod("GET"), enpoint));

            Assert.Equal(HttpStatusCode.OK, searchResult.StatusCode);

            var usersJson = JObject.Parse(await searchResult.Content.ReadAsStringAsync());
            var users = usersJson.ToObject<SearchResultApiModel<FabricPrincipalApiModel>>();

            Assert.Equal(1, users.ResultCount);
            Assert.Single(users.Principals.Select(p => p.IdentityProvider.Equals("Azure")));
            Assert.Single(users.Principals.Select(p => p.PrincipalType.Equals("user")));
            Assert.Single(users.Principals.Select(p => p.TenantId.Equals("TenantId")));
            Assert.Single(users.Principals.Select(p => p.IdentityProviderUserPrincipalName.Equals("UserPrincipal")));
        }

        [Fact]
        public async Task SearchPrincipals_FindAzureUser_BadIdentityProvider_BadRequest_Async()
        {
            var searchQuery = "searchtext=johnny";
            var typeQuery = "type=user";
            var tenantQuery = "tenantid=2";
            var enpoint = $"{_principalsBaseUrl}/invalidIdP/search?{searchQuery}&{typeQuery}&{tenantQuery}";

            var httpClient = await HttpClient;
            var searchResult = await httpClient.SendAsync(new HttpRequestMessage(new HttpMethod("GET"), enpoint));

            Assert.Equal(HttpStatusCode.BadRequest, searchResult.StatusCode);
        }

        [Fact]
        public async Task SearchPrincipals_FindAzureUsers_Succeeds_Async()
        {
            var searchQuery = "searchtext=johnny";
            var typeQuery = "type=user";
            var tenantQuery = "tenantid=1";
            var enpoint = $"{_principalsBaseUrl}/AzureActiveDirectory/search?{searchQuery}&{typeQuery}&{tenantQuery}";

            var httpClient = await HttpClient;
            var searchResult = await httpClient.SendAsync(new HttpRequestMessage(new HttpMethod("GET"), enpoint));

            Assert.Equal(HttpStatusCode.OK, searchResult.StatusCode);

            var usersJson = JObject.Parse(await searchResult.Content.ReadAsStringAsync());
            var users = usersJson.ToObject<SearchResultApiModel<FabricPrincipalApiModel>>();

            Assert.Equal(2, users.ResultCount);
            Assert.Equal(2, users.Principals.Select(p => p.IdentityProvider.Equals("Azure")).Count());
            Assert.Equal(2, users.Principals.Select(p => p.PrincipalType.Equals("user")).Count());
            Assert.Equal(2, users.Principals.Select(p => p.TenantId.Equals("TenantId")).Count());
            Assert.Equal(2, users.Principals.Select(p => p.IdentityProviderUserPrincipalName.Equals("UserPrincipal")).Count());
        }

        [Fact]
        public async Task SearchPrincipals_FindUsers_Succeeds_Async()
        {
            var searchQuery = "searchtext=johnny";
            var typeQuery = "type=user";
            var tenantQuery = "tenantid=1";
            var enpoint = $"{_principalsBaseUrl}/search?{searchQuery}&{typeQuery}&{tenantQuery}";

            var httpClient = await HttpClient;
            var searchResult = await httpClient.SendAsync(new HttpRequestMessage(new HttpMethod("GET"), enpoint));

            Assert.Equal(HttpStatusCode.OK, searchResult.StatusCode);

            var usersJson = JObject.Parse(await searchResult.Content.ReadAsStringAsync());
            var users = usersJson.ToObject<SearchResultApiModel<FabricPrincipalApiModel>>();

            Assert.Equal(2, users.ResultCount);
            Assert.Equal(2, users.Principals.Select(p => p.IdentityProvider.Equals("Azure")).Count());
            Assert.Equal(2, users.Principals.Select(p => p.PrincipalType.Equals("user")).Count());
            Assert.Equal(2, users.Principals.Select(p => p.TenantId.Equals("TenantId")).Count());
            Assert.Equal(2, users.Principals.Select(p => p.IdentityProviderUserPrincipalName.Equals("UserPrincipal")).Count());
        }

        [Fact]
        public async Task SearchPrincipalsByGroup_FindGroup_Succeeds_Async()
        {
            var enpoint = $"{_principalsBaseUrl}/AzureActiveDirectory/groups/IT";

            var httpClient = await HttpClient;
            var searchResult = await httpClient.SendAsync(new HttpRequestMessage(new HttpMethod("GET"), enpoint));

            Assert.Equal(HttpStatusCode.OK, searchResult.StatusCode);

            var groupsJson = JObject.Parse(await searchResult.Content.ReadAsStringAsync());
            var groups = groupsJson.ToObject<SearchResultApiModel<FabricGroupApiModel>>();

            Assert.Equal(1, groups.ResultCount);
            Assert.Single(groups.Principals.Select(p => p.IdentityProvider.Equals("Azure")));
            Assert.Single(groups.Principals.Select(p => p.GroupName.Equals("IT")));
            Assert.Single(groups.Principals.Select(p => p.PrincipalType.Equals("group")));
        }

        [Fact]
        public async Task SearchPrincipalsByGroup_FindGroupByTenant_Succeeds_Async()
        {
            var tenantQuery = "tenantid=2";
            var enpoint = $"{_principalsBaseUrl}/AzureActiveDirectory/groups/IT?{tenantQuery}";

            var httpClient = await HttpClient;
            var searchResult = await httpClient.SendAsync(new HttpRequestMessage(new HttpMethod("GET"), enpoint));

            Assert.Equal(HttpStatusCode.OK, searchResult.StatusCode);

            var groupsJson = JObject.Parse(await searchResult.Content.ReadAsStringAsync());
            var groups = groupsJson.ToObject<SearchResultApiModel<FabricGroupApiModel>>();

            Assert.Equal(1, groups.ResultCount);
            Assert.Single(groups.Principals.Select(p => p.IdentityProvider.Equals("Azure")));
            Assert.Single(groups.Principals.Select(p => p.GroupName.Equals("IT")));
            Assert.Single(groups.Principals.Select(p => p.TenantId.Equals("2")));
            Assert.Single(groups.Principals.Select(p => p.PrincipalType.Equals("group")));
        }

        [Fact]
        public async Task SearchPrincipalsByGroup_BadIdentityProvider_BadRequest_Async()
        {
            var tenantQuery = "tenantid=2";
            var enpoint = $"{_principalsBaseUrl}/invalidIdP/groups/IT?{tenantQuery}";

            var httpClient = await HttpClient;
            var searchResult = await httpClient.SendAsync(new HttpRequestMessage(new HttpMethod("GET"), enpoint));

            Assert.Equal(HttpStatusCode.BadRequest, searchResult.StatusCode);
        }

        [Fact]
        public async Task SearchPrincipals_FindUserNoTenant_Succeeds_Async()
        {
            var subjectQuery = $"subjectId={_appConfig.DomainName}\\james rocket";
            var enpoint = $"{_principalsBaseUrl}/user?{subjectQuery}";

            var httpClient = await HttpClient;
            var searchResult = await httpClient.SendAsync(new HttpRequestMessage(new HttpMethod("GET"), enpoint));

            Assert.Equal(HttpStatusCode.OK, searchResult.StatusCode);

            var userJson = JObject.Parse(await searchResult.Content.ReadAsStringAsync());
            var user = userJson.ToObject<FabricPrincipalApiModel>();

            Assert.Equal("user", user.PrincipalType);
            Assert.Equal("james.rocket@email.com", user.IdentityProviderUserPrincipalName);
        }

        [Fact]
        public async Task SearchPrincipals_FindUser_Succeeds_Async()
        {
            var subjectQuery = $"subjectId={_appConfig.DomainName}\\james rocket";
            var tenantQuery = "tenantid=1";
            var enpoint = $"{_principalsBaseUrl}/user?{subjectQuery}&{tenantQuery}";

            var httpClient = await HttpClient;
            var searchResult = await httpClient.SendAsync(new HttpRequestMessage(new HttpMethod("GET"), enpoint));

            Assert.Equal(HttpStatusCode.OK, searchResult.StatusCode);

            var userJson = JObject.Parse(await searchResult.Content.ReadAsStringAsync());
            var user = userJson.ToObject<FabricPrincipalApiModel>();

            Assert.Equal("user", user.PrincipalType);
            Assert.Equal("1", user.TenantId);
            Assert.Equal("james.rocket@email.com", user.IdentityProviderUserPrincipalName);
        }
    }
}
