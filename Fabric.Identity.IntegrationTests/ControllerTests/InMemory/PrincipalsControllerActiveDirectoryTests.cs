using Fabric.Identity.API;
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
    public class PrincipalsControllerActiveDirectoryTests : IntegrationTestsFixture
    {
        private readonly string _principalsBaseUrl = "/api/principals";

        public PrincipalsControllerActiveDirectoryTests(string provider = FabricIdentityConstants.StorageProviders.InMemory) : base(provider)
        { }

        [Fact]
        public async Task SearchPrincipals_FindGroups_Succeeds_Async()
        {
            var searchQuery = "searchtext=ITGroup";
            var typeQuery = "type=group";
            var enpoint = $"{_principalsBaseUrl}/Windows/search?{searchQuery}&{typeQuery}";

            var httpClient = await HttpClient;
            var searchResult = await httpClient.SendAsync(new HttpRequestMessage(new HttpMethod("GET"), enpoint));

            Assert.Equal(HttpStatusCode.OK, searchResult.StatusCode);

            var groupsJson = JObject.Parse(await searchResult.Content.ReadAsStringAsync());
            var groups = groupsJson.ToObject<SearchResultApiModel<FabricPrincipalApiModel>>();

            Assert.Equal(4, groups.ResultCount);
            Assert.Equal(4, groups.Principals.Select(p => p.PrincipalType.Equals("group")).Count());
        }

        [Fact]
        public async Task SearchPrincipals_FindUsersAndGroups_Succeeds_Async()
        {
            var searchQuery = "searchtext=ITGroup";
            var typeQuery = "type=group";
            var enpoint = $"{_principalsBaseUrl}/Windows/search?{searchQuery}&{typeQuery}";

            var httpClient = await HttpClient;
            var searchResult = await httpClient.SendAsync(new HttpRequestMessage(new HttpMethod("GET"), enpoint));

            Assert.Equal(HttpStatusCode.OK, searchResult.StatusCode);

            var groupsJson = JObject.Parse(await searchResult.Content.ReadAsStringAsync());
            var groups = groupsJson.ToObject<SearchResultApiModel<FabricPrincipalApiModel>>();

            var searchResult = await _browser.Get("/principals/search", with =>
            {
                with.HttpRequest();
                with.Query("searchtext", "pat");
            });

            Assert.Equal(HttpStatusCode.OK, searchResult.StatusCode);

            var users = searchResult.Body.DeserializeJson<IdpSearchResultApiModel<FabricPrincipalApiModel>>();
            Assert.Equal(3, users.ResultCount);
            Assert.Equal(2, users.Principals.Count(p => p.PrincipalType.Equals("user")));
            Assert.Equal(1, users.Principals.Count(p => p.PrincipalType.Equals("group")));
        }

        [Fact]
        public async Task SearchPrincipals_FindGroups_Succeeds_Async()
        {
            var searchQuery = "searchtext=pat";
            var typeQuery = "type=group";
            var enpoint = $"{_principalsBaseUrl}/search?{searchQuery}&{typeQuery}";

            var httpClient = await HttpClient;
            var searchResult = await httpClient.SendAsync(new HttpRequestMessage(new HttpMethod("GET"), enpoint));

            Assert.Equal(HttpStatusCode.OK, searchResult.StatusCode);

            var groupsJson = JObject.Parse(await searchResult.Content.ReadAsStringAsync());
            var groups = groupsJson.ToObject<SearchResultApiModel<FabricPrincipalApiModel>>();

            var searchResult = await _browser.Get("/principals/search", with =>
            {
                with.HttpRequest();
                with.Query("searchtext", "pat");
                with.Query("type", "group");
            });

            Assert.Equal(HttpStatusCode.OK, searchResult.StatusCode);

            var users = searchResult.Body.DeserializeJson<IdpSearchResultApiModel<FabricPrincipalApiModel>>();
            Assert.Equal(1, users.ResultCount);
            Assert.Equal(1, users.Principals.Count(p => p.PrincipalType.Equals("group")));
        }

        [Fact]
        public async Task SearchPrincipals_FindUsers_Succeeds_Async()
        {
            var searchResult = await _browser.Get("/principals/search", with =>
            {
                with.HttpRequest();
                with.Query("searchtext", "pat");
                with.Query("type", "user");
            });

            Assert.Equal(HttpStatusCode.OK, searchResult.StatusCode);

            var users = searchResult.Body.DeserializeJson<IdpSearchResultApiModel<FabricPrincipalApiModel>>();
            Assert.Equal(2, users.ResultCount);
            Assert.Equal(2, users.Principals.Count(p => p.PrincipalType.Equals("user")));
            Assert.Equal(2, users.Principals.Select(p => p.IdentityProviderUserPrincipalName.Equals("subjectId")).Count());
        }

        [Fact]
        public async Task SearchPrincipals_NoPrincipalsFound_Succeeds_Async()
        {
            var searchResult = await _browser.Get("/principals/search", with =>
            {
                with.HttpRequest();
                with.Query("searchtext", "fdfd");
            });

            Assert.Equal(HttpStatusCode.OK, searchResult.StatusCode);

            var users = searchResult.Body.DeserializeJson<IdpSearchResultApiModel<FabricPrincipalApiModel>>();
            Assert.Equal(0, users.ResultCount);
        }

        [Fact]
        public async Task SearchPrincipals_NoSearchText_Fails_Async()
        {
            var searchResult = await _browser.Get("/principals/search", with =>
            {
                with.HttpRequest();
            });

            Assert.Equal(HttpStatusCode.BadRequest, searchResult.StatusCode);
        }

        [Fact]
        public async Task SearchPrincipals_InvalidType_Fails_Async()
        {
            var searchResult = await _browser.Get("/principals/search", with =>
            {
                with.HttpRequest();
                with.Query("type", "foo");
            });

            Assert.Equal(HttpStatusCode.BadRequest, searchResult.StatusCode);
        }

        [Fact]
        public async Task SearchPrincipals_FindUsersByFullName_Succeeds_Async()
        {
            var searchResult = await _browser.Get("/principals/search", with =>
            {
                with.HttpRequest();
                with.Query("searchtext", "patrick jones");
                with.Query("type", "user");
            });

            Assert.Equal(HttpStatusCode.OK, searchResult.StatusCode);

            var users = searchResult.Body.DeserializeJson<IdpSearchResultApiModel<FabricPrincipalApiModel>>();
            Assert.Equal(1, users.ResultCount);
            Assert.Equal(1, users.Principals.Count(p => p.PrincipalType.Equals("user")));
        }

        [Fact]
        public async Task SearchPrincipals_FindUser_Succeeds_Async()
        {
            var searchResult = await _browser.Get("/principals/user", with =>
            {
                with.HttpRequest();
                with.Query("subjectId", $"{ _appConfig.DomainName}\\patrick.jones");
            });

            Assert.Equal(HttpStatusCode.OK, searchResult.StatusCode);

            var user = searchResult.Body.DeserializeJson<FabricPrincipalApiModel>();
            Assert.Equal("user", user.PrincipalType);
            Assert.Equal(user.SubjectId, user.IdentityProviderUserPrincipalName);
            Assert.Equal("patrick.jones@email.com", user.Email);
        }

        [Fact]
        public async Task SearchPrincipals_FindIdentityProviderUsers_Succeeds_Async()
        {
            var searchResult = await _browser.Get("/principals/" + IdentityProviders.ActiveDirectory + "/search", with =>
            {
                with.HttpRequest();
                with.Query("searchtext", "pat");
                with.Query("type", "user");
            });

            Assert.Equal(HttpStatusCode.OK, searchResult.StatusCode);

            var users = searchResult.Body.DeserializeJson<IdpSearchResultApiModel<FabricPrincipalApiModel>>();
            Assert.Equal(2, users.ResultCount);
            Assert.Equal(2, users.Principals.Count(p => p.PrincipalType.Equals("user")));
            Assert.Equal(2, users.Principals.Select(p => p.IdentityProviderUserPrincipalName.Equals("subjectId")).Count());
        }

        [Fact]
        public async Task SearchPrincipals_FindIdentityProviderUsersAndGroups_Succeeds_Async()
        {

            var searchResult = await _browser.Get("/principals/" + IdentityProviders.ActiveDirectory + "/search", with =>
            {
                with.HttpRequest();
                with.Query("searchtext", "pat");
            });

            Assert.Equal(HttpStatusCode.OK, searchResult.StatusCode);

            var usersAndGroups = searchResult.Body.DeserializeJson<IdpSearchResultApiModel<FabricPrincipalApiModel>>();
            var userList = usersAndGroups.Principals.Where(p => p.PrincipalType.Equals("user"));
            var groupList = usersAndGroups.Principals.Where(p => p.PrincipalType.Equals("group"));

            Assert.Equal(2, userList.Count());
            Assert.Single(groupList);
            Assert.Equal(2, userList.Select(p => p.IdentityProviderUserPrincipalName.Equals("SubjectId")).Count());
            Assert.Null(groupList.Select(p => p.IdentityProviderUserPrincipalName).Single());
        }
    }
}
