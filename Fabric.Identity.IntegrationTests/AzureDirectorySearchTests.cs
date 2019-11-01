using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Xunit;
using System.Net;
using Fabric.Identity.IntegrationTests;
using Fabric.Identity.API.Configuration;
using Fabric.Identity.API.Models;

namespace Fabric.Identity.IntegrationTests
{
    public class AzureDirectorySearchTests : IClassFixture<IntegrationTestsFixture>
    {
        private readonly IntegrationTestsFixture _integrationTestsFixture;
        private readonly Browser _browser;
        private readonly string identityProvider = IdentityProviders.AzureActiveDirectory;
        private readonly AppConfiguration _appConfig;
        public AzureDirectorySearchTests(IntegrationTestsFixture integrationTestsFixture)
        {
            _integrationTestsFixture = integrationTestsFixture;
            var claimsPrincipal = new ClaimsPrincipal(new ClaimsIdentity(new List<Claim>
            {
                new Claim("scope", Scopes.SearchPrincipalsScope)
            }, "testprincipal"));

            _browser = integrationTestsFixture.GetBrowser(claimsPrincipal, identityProvider);

            _appConfig = new AppConfiguration
            {
                DomainName = "testingAzure"
            };
        }

        [Fact]
        public async Task SearchPrincipals_FindGroups_Succeeds_Async()
        {
            var searchResult = await _browser.Get("/principals/" + IdentityProviders.AzureActiveDirectory + "/search", with =>
            {
                with.HttpRequest();
                with.Query("searchtext", "ITGroup");
                with.Query("type", "group");
            });

            Assert.Equal(HttpStatusCode.OK, searchResult.StatusCode);

            var groups = searchResult.Body.DeserializeJson<IdpSearchResultApiModel<FabricPrincipalApiModel>>();
            Assert.Equal(4, groups.ResultCount);
            Assert.Equal(4, groups.Principals.Select(p => p.IdentityProvider.Equals("Azure")).Count());
            Assert.Equal(4, groups.Principals.Select(p => p.PrincipalType.Equals("group")).Count());
        }

        [Fact]
        public async Task SearchPrincipals_FindGroupsWithTenant_Succeeds_Async()
        {
            var searchResult = await _browser.Get("/principals/" + IdentityProviders.AzureActiveDirectory + "/search", with =>
            {
                with.HttpRequest();
                with.Query("searchtext", "ITGroup");
                with.Query("type", "group");
                with.Query("tenantid", "1");
            });

            Assert.Equal(HttpStatusCode.OK, searchResult.StatusCode);

            var groups = searchResult.Body.DeserializeJson<IdpSearchResultApiModel<FabricPrincipalApiModel>>();
            Assert.Equal(4, groups.ResultCount);
            Assert.Equal(4, groups.Principals.Select(p => p.IdentityProvider.Equals("Azure")).Count());
            Assert.Equal(4, groups.Principals.Select(p => p.PrincipalType.Equals("group")).Count());
            Assert.Equal(4, groups.Principals.Select(p => p.TenantId.Equals("TenantId")).Count());
        }

        [Fact]
        public async Task SearchPrincipals_FindAzureUsersNoTenant_Succeeds_Async()
        {
            var searchResult = await _browser.Get("/principals/" + IdentityProviders.AzureActiveDirectory + "/search", with =>
            {
                with.HttpRequest();
                with.Query("searchtext", "johnny");
                with.Query("type", "user");
            });

            Assert.Equal(HttpStatusCode.OK, searchResult.StatusCode);

            var users = searchResult.Body.DeserializeJson<IdpSearchResultApiModel<FabricPrincipalApiModel>>();
            Assert.Equal(3, users.ResultCount);
            Assert.Equal(3, users.Principals.Select(p => p.IdentityProvider.Equals("Azure")).Count());
            Assert.Equal(3, users.Principals.Select(p => p.PrincipalType.Equals("user")).Count());
            Assert.Equal(3, users.Principals.Select(p => p.TenantId.Equals("TenantId")).Count());
            Assert.Equal(3, users.Principals.Select(p => p.IdentityProviderUserPrincipalName.Equals("UserPrincipal")).Count());
        }

        [Fact]
        public async Task SearchPrincipals_FindAzureUser_Succeeds_Async()
        {
            var searchResult = await _browser.Get("/principals/" + IdentityProviders.AzureActiveDirectory + "/search", with =>
            {
                with.HttpRequest();
                with.Query("searchtext", "johnny");
                with.Query("type", "user");
                with.Query("tenantid", "2");
            });

            Assert.Equal(HttpStatusCode.OK, searchResult.StatusCode);

            var users = searchResult.Body.DeserializeJson<IdpSearchResultApiModel<FabricPrincipalApiModel>>();
            Assert.Equal(1, users.ResultCount);
            Assert.Single(users.Principals.Select(p => p.IdentityProvider.Equals("Azure")));
            Assert.Single(users.Principals.Select(p => p.PrincipalType.Equals("user")));
            Assert.Single(users.Principals.Select(p => p.TenantId.Equals("TenantId")));
            Assert.Single(users.Principals.Select(p => p.IdentityProviderUserPrincipalName.Equals("UserPrincipal")));
        }

        [Fact]
        public async Task SearchPrincipals_FindAzureUser_BadIdentityProvider_BadRequest_Async()
        {
            var searchResult = await _browser.Get("/principals/invalidIdP/search", with =>
            {
                with.HttpRequest();
                with.Query("searchtext", "johnny");
                with.Query("type", "user");
                with.Query("tenantid", "2");
            });

            Assert.Equal(HttpStatusCode.BadRequest, searchResult.StatusCode);
        }

        [Fact]
        public async Task SearchPrincipals_FindAzureUsers_Succeeds_Async()
        {
            var searchResult = await _browser.Get("/principals/" + IdentityProviders.AzureActiveDirectory + "/search", with =>
            {
                with.HttpRequest();
                with.Query("searchtext", "johnny");
                with.Query("type", "user");
                with.Query("tenantid", "1");
            });

            Assert.Equal(HttpStatusCode.OK, searchResult.StatusCode);

            var users = searchResult.Body.DeserializeJson<IdpSearchResultApiModel<FabricPrincipalApiModel>>();
            Assert.Equal(2, users.ResultCount);
            Assert.Equal(2, users.Principals.Select(p => p.IdentityProvider.Equals("Azure")).Count());
            Assert.Equal(2, users.Principals.Select(p => p.PrincipalType.Equals("user")).Count());
            Assert.Equal(2, users.Principals.Select(p => p.TenantId.Equals("TenantId")).Count());
            Assert.Equal(2, users.Principals.Select(p => p.IdentityProviderUserPrincipalName.Equals("UserPrincipal")).Count());
        }

        [Fact]
        public async Task SearchPrincipals_FindUsers_Succeeds_Async()
        {
            var searchResult = await _browser.Get("/principals/search", with =>
            {
                with.HttpRequest();
                with.Query("searchtext", "johnny");
                with.Query("type", "user");
                with.Query("tenantid", "1");
            });

            Assert.Equal(HttpStatusCode.OK, searchResult.StatusCode);

            var users = searchResult.Body.DeserializeJson<IdpSearchResultApiModel<FabricPrincipalApiModel>>();
            Assert.Equal(2, users.ResultCount);
            Assert.Equal(2, users.Principals.Select(p => p.IdentityProvider.Equals("Azure")).Count());
            Assert.Equal(2, users.Principals.Select(p => p.PrincipalType.Equals("user")).Count());
            Assert.Equal(2, users.Principals.Select(p => p.TenantId.Equals("TenantId")).Count());
            Assert.Equal(2, users.Principals.Select(p => p.IdentityProviderUserPrincipalName.Equals("UserPrincipal")).Count());
        }

        [Fact]
        public async Task SearchPrincipalsByGroup_FindGroup_Succeeds_Async()
        {
            var searchResult = await _browser.Get("/principals/" + IdentityProviders.AzureActiveDirectory + "/groups/IT", with =>
            {
                with.HttpRequest();
            });

            Assert.Equal(HttpStatusCode.OK, searchResult.StatusCode);

            var groups = searchResult.Body.DeserializeJson<IdpSearchResultApiModel<FabricGroupApiModel>>();
            Assert.Equal(1, groups.ResultCount);
            Assert.Single(groups.Principals.Select(p => p.IdentityProvider.Equals("Azure")));
            Assert.Single(groups.Principals.Select(p => p.GroupName.Equals("IT")));
            Assert.Single(groups.Principals.Select(p => p.PrincipalType.Equals("group")));
        }

        [Fact]
        public async Task SearchPrincipalsByGroup_FindGroupByTenant_Succeeds_Async()
        {
            var searchResult = await _browser.Get("/principals/" + IdentityProviders.AzureActiveDirectory + "/groups/IT", with =>
            {
                with.HttpRequest();
                with.Query("tenantid", "2");
            });

            Assert.Equal(HttpStatusCode.OK, searchResult.StatusCode);

            var groups = searchResult.Body.DeserializeJson<IdpSearchResultApiModel<FabricGroupApiModel>>();
            Assert.Equal(1, groups.ResultCount);
            Assert.Single(groups.Principals.Select(p => p.IdentityProvider.Equals("Azure")));
            Assert.Single(groups.Principals.Select(p => p.GroupName.Equals("IT")));
            Assert.Single(groups.Principals.Select(p => p.TenantId.Equals("2")));
            Assert.Single(groups.Principals.Select(p => p.PrincipalType.Equals("group")));
        }

        [Fact]
        public async Task SearchPrincipalsByGroup_BadIdentityProvider_BadRequest_Async()
        {
            var searchResult = await _browser.Get("/principals/invalidIdP/groups/IT", with =>
            {
                with.HttpRequest();
                with.Query("tenantid", "2");
            });

            Assert.Equal(HttpStatusCode.BadRequest, searchResult.StatusCode);
        }

        [Fact]
        public async Task SearchPrincipals_FindUserNoTenant_Succeeds_Async()
        {
            var searchResult = await _browser.Get("/principals/user", with =>
            {
                with.HttpRequest();
                with.Query("subjectId", $"{ _appConfig.DomainName}\\james rocket");
            });

            Assert.Equal(HttpStatusCode.OK, searchResult.StatusCode);

            var user = searchResult.Body.DeserializeJson<FabricPrincipalApiModel>();
            Assert.Equal("user", user.PrincipalType);
            Assert.Equal("james.rocket@email.com", user.IdentityProviderUserPrincipalName);
        }

        [Fact]
        public async Task SearchPrincipals_FindUser_Succeeds_Async()
        {
            var searchResult = await _browser.Get("/principals/user", with =>
            {
                with.HttpRequest();
                with.Query("subjectId", $"{ _appConfig.DomainName}\\james rocket");
                with.Query("tenantid", "1");
            });

            Assert.Equal(HttpStatusCode.OK, searchResult.StatusCode);

            var user = searchResult.Body.DeserializeJson<FabricPrincipalApiModel>();
            Assert.Equal("user", user.PrincipalType);
            Assert.Equal("1", user.TenantId);
            Assert.Equal("james.rocket@email.com", user.IdentityProviderUserPrincipalName);
        }
    }
}
