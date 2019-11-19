using Fabric.Identity.API;
using Fabric.Identity.API.Models;
using Newtonsoft.Json.Linq;
using System.Linq;
using System.Net;
using System.Net.Http;
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
            var searchQuery = "searchtext=pat";
            var typeQuery = "type=group";
            var endpoint = $"{_principalsBaseUrl}/search?{searchQuery}&{typeQuery}";

            var httpClient = await HttpClient;
            var searchResult = await httpClient.SendAsync(new HttpRequestMessage(new HttpMethod("GET"), endpoint));

            Assert.Equal(HttpStatusCode.OK, searchResult.StatusCode);

            var groupsJson = JObject.Parse(await searchResult.Content.ReadAsStringAsync());
            var groups = groupsJson.ToObject<SearchResultApiModel<FabricPrincipalApiModel>>();

            Assert.Equal(1, groups.ResultCount);
            Assert.Equal(1, groups.Principals.Select(p => p.PrincipalType.Equals("group")).Count());
        }

        [Fact]
        public async Task SearchPrincipals_FindUsersAndGroups_Succeeds_Async()
        {
            var searchQuery = "searchtext=pat";
            var endpoint = $"{_principalsBaseUrl}/search?{searchQuery}";

            var httpClient = await HttpClient;
            var searchResult = await httpClient.SendAsync(new HttpRequestMessage(new HttpMethod("GET"), endpoint));

            Assert.Equal(HttpStatusCode.OK, searchResult.StatusCode);

            var resultsJson = JObject.Parse(await searchResult.Content.ReadAsStringAsync());
            var results = resultsJson.ToObject<SearchResultApiModel<FabricPrincipalApiModel>>();

            Assert.Equal(3, results.ResultCount);
            Assert.Equal(2, results.Principals.Count(p => p.PrincipalType.Equals("user")));
            Assert.Equal(1, results.Principals.Count(p => p.PrincipalType.Equals("group")));
        }

        [Fact]
        public async Task SearchPrincipals_FindUsers_Succeeds_Async()
        {
            var searchQuery = "searchtext=pat";
            var typeQuery = "type=user";
            var endpoint = $"{_principalsBaseUrl}/search?{searchQuery}&{typeQuery}";

            var httpClient = await HttpClient;
            var searchResult = await httpClient.SendAsync(new HttpRequestMessage(new HttpMethod("GET"), endpoint));

            Assert.Equal(HttpStatusCode.OK, searchResult.StatusCode);

            var resultsJson = JObject.Parse(await searchResult.Content.ReadAsStringAsync());
            var results = resultsJson.ToObject<SearchResultApiModel<FabricPrincipalApiModel>>();

            Assert.Equal(2, results.ResultCount);
            Assert.Equal(2, results.Principals.Count(p => p.PrincipalType.Equals("user")));
            Assert.Equal(2, results.Principals.Select(p => p.IdentityProviderUserPrincipalName.Equals("subjectId")).Count());
        }

        [Fact]
        public async Task SearchPrincipals_NoPrincipalsFound_Succeeds_Async()
        {
            var searchQuery = "searchtext=fdfdfd";
            var endpoint = $"{_principalsBaseUrl}/search?{searchQuery}";

            var httpClient = await HttpClient;
            var searchResult = await httpClient.SendAsync(new HttpRequestMessage(new HttpMethod("GET"), endpoint));

            Assert.Equal(HttpStatusCode.OK, searchResult.StatusCode);

            var resultsJson = JObject.Parse(await searchResult.Content.ReadAsStringAsync());
            var results = resultsJson.ToObject<SearchResultApiModel<FabricPrincipalApiModel>>();
            Assert.Equal(0, results.ResultCount);
        }

        [Fact]
        public async Task SearchPrincipals_NoSearchText_Fails_Async()
        {
            var endpoint = $"{_principalsBaseUrl}/search";

            var httpClient = await HttpClient;
            var searchResult = await httpClient.SendAsync(new HttpRequestMessage(new HttpMethod("GET"), endpoint));

            Assert.Equal(HttpStatusCode.BadRequest, searchResult.StatusCode);
        }

        [Fact]
        public async Task SearchPrincipals_InvalidType_Fails_Async()
        {
            var endpoint = $"{_principalsBaseUrl}/search?type=invalidType";

            var httpClient = await HttpClient;
            var searchResult = await httpClient.SendAsync(new HttpRequestMessage(new HttpMethod("GET"), endpoint));

            Assert.Equal(HttpStatusCode.BadRequest, searchResult.StatusCode);
        }

        [Fact]
        public async Task SearchPrincipals_FindUsersByFullName_Succeeds_Async()
        {
            var searchQuery = "searchtext=patrick+jones";
            var typeQuery = "type=user";
            var endpoint = $"{_principalsBaseUrl}/search?{searchQuery}&{typeQuery}";

            var httpClient = await HttpClient;
            var searchResult = await httpClient.SendAsync(new HttpRequestMessage(new HttpMethod("GET"), endpoint));

            Assert.Equal(HttpStatusCode.OK, searchResult.StatusCode);

            var resultsJson = JObject.Parse(await searchResult.Content.ReadAsStringAsync());
            var results = resultsJson.ToObject<SearchResultApiModel<FabricPrincipalApiModel>>();

            Assert.Equal(1, results.ResultCount);
            Assert.Equal(1, results.Principals.Count(p => p.PrincipalType.Equals("user")));
        }

        [Fact]
        public async Task SearchPrincipals_FindUser_Succeeds_Async()
        {
            var subjectIdQuery = "subjectId=testing\\patrick.jones";
            var endpoint = $"{_principalsBaseUrl}/user?{subjectIdQuery}";

            var httpClient = await HttpClient;
            var searchResult = await httpClient.SendAsync(new HttpRequestMessage(new HttpMethod("GET"), endpoint));

            Assert.Equal(HttpStatusCode.OK, searchResult.StatusCode);

            var resultJson = JObject.Parse(await searchResult.Content.ReadAsStringAsync());
            var result = resultJson.ToObject<FabricPrincipalApiModel>();

            Assert.Equal("user", result.PrincipalType);
            Assert.Equal(result.SubjectId, result.IdentityProviderUserPrincipalName);
            Assert.Equal("patrick.jones@email.com", result.Email);
        }

        [Fact]
        public async Task SearchPrincipals_FindIdentityProviderUsers_Succeeds_Async()
        {
            var searchQuery = "searchtext=pat";
            var typeQuery = "type=user";
            var endpoint = $"{_principalsBaseUrl}/Windows/search?{searchQuery}&{typeQuery}";

            var httpClient = await HttpClient;
            var searchResult = await httpClient.SendAsync(new HttpRequestMessage(new HttpMethod("GET"), endpoint));

            Assert.Equal(HttpStatusCode.OK, searchResult.StatusCode);

            var resultsJson = JObject.Parse(await searchResult.Content.ReadAsStringAsync());
            var results = resultsJson.ToObject<SearchResultApiModel<FabricPrincipalApiModel>>();

            Assert.Equal(2, results.ResultCount);
            Assert.Equal(2, results.Principals.Count(p => p.PrincipalType.Equals("user")));
            Assert.Equal(2, results.Principals.Select(p => p.IdentityProviderUserPrincipalName.Equals("subjectId")).Count());
        }

        [Fact]
        public async Task SearchPrincipals_FindIdentityProviderUsersAndGroups_Succeeds_Async()
        {

            var searchQuery = "searchtext=pat";
            var endpoint = $"{_principalsBaseUrl}/Windows/search?{searchQuery}";

            var httpClient = await HttpClient;
            var searchResult = await httpClient.SendAsync(new HttpRequestMessage(new HttpMethod("GET"), endpoint));

            Assert.Equal(HttpStatusCode.OK, searchResult.StatusCode);

            var resultsJson = JObject.Parse(await searchResult.Content.ReadAsStringAsync());
            var results = resultsJson.ToObject<SearchResultApiModel<FabricPrincipalApiModel>>();

            var userList = results.Principals.Where(p => p.PrincipalType.Equals("user"));
            var groupList = results.Principals.Where(p => p.PrincipalType.Equals("group"));

            Assert.Equal(2, userList.Count());
            Assert.Single(groupList);
            Assert.Equal(2, userList.Select(p => p.IdentityProviderUserPrincipalName.Equals("SubjectId")).Count());
            Assert.Null(groupList.Select(p => p.IdentityProviderUserPrincipalName).Single());
        }
    }
}
