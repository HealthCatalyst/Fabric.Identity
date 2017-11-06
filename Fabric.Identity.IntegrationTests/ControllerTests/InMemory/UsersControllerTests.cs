using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Fabric.Identity.API;
using Fabric.Identity.API.Models;
using Fabric.Identity.API.Persistence;
using Fabric.Identity.API.Persistence.InMemory.Services;
using Fabric.Identity.API.Persistence.InMemory.Stores;
using Fabric.Identity.API.Services;
using Fabric.Identity.IntegrationTests.ServiceTests;
using Moq;
using Newtonsoft.Json;
using Serilog;
using Xunit;

namespace Fabric.Identity.IntegrationTests.ControllerTests.InMemory
{
    public class UsersControllerTests : IntegrationTestsFixture
    {
        public UsersControllerTests(string provider = FabricIdentityConstants.StorageProviders.InMemory) : base(provider)
        {
            UserStore = new InMemoryUserStore(new InMemoryDocumentService());
        }

        private readonly ILogger _logger = new Mock<ILogger>().Object;
        protected IUserStore UserStore { get; set; }
        private readonly string _usersSearchApiBaseUrl = "/api/users";
        private readonly string _identityProviderSearchBaseUrl = "/api/users/search";

        private void CreateNewUser(User user)
        {
            UserStore.AddUserAsync(user);
        }

        private static readonly Random Random = new Random();

        private static string GetRandomString()
        {
            var path = Path.GetRandomFileName();
            path = path.Replace(".", "");

            var stringLength = Random.Next(5, path.Length);

            return path.Substring(0, stringLength);
        }

        private string CreateUsersAndQuery(int numberToCreate, string clientId, bool halfWithoutLoginForClient = false)
        {
            var queryBuilder = new StringBuilder($"?clientId={clientId}&userIds=");

            for (var i = 1; i <= numberToCreate; i++)
            {
                var user = new User
                {
                    SubjectId = $"{GetRandomString()}\\{GetRandomString()}",
                    Username = i.ToString(),
                    ProviderName = FabricIdentityConstants.FabricExternalIdentityProviderTypes.Windows
                };

                if (halfWithoutLoginForClient && i % 2 == 0)
                {
                    user.SetLastLoginDateForClient(clientId);
                }
                else if (!halfWithoutLoginForClient)
                {
                    user.SetLastLoginDateForClient(clientId);
                }

                CreateNewUser(user);
                var seperator = i == numberToCreate ? string.Empty : ",";
                queryBuilder.Append($"{user.SubjectId}:{user.ProviderName}{seperator}");
            }

            return queryBuilder.ToString();
        }

        [Fact]
        public async Task UsersController_Get_FindsUsersByDocumentId_LastLoginForClientSet()
        {
            var numberOfUsers = 10;
            var usersQuery = CreateUsersAndQuery(numberOfUsers, TestClientName);
            var response =
                await HttpClient.SendAsync(new HttpRequestMessage(new HttpMethod("GET"),
                    $"{_usersSearchApiBaseUrl}{usersQuery}"));

            var content = await response.Content.ReadAsStringAsync();

            var resource = (List<UserApiModel>) JsonConvert.DeserializeObject(content, typeof(List<UserApiModel>));
            Assert.Equal(numberOfUsers, resource.Count);
            foreach (var userApiModel in resource)
            {
                Assert.True(userApiModel.LastLoginDate.HasValue);
            }
        }

        [Fact]
        public async Task UsersController_Get_FindUsersByDocumentId_InvalidClientId_BadRequest()
        {
            var numberOfUsers = 1;
            var usersQuery = CreateUsersAndQuery(numberOfUsers, "foo");
            var response =
                await HttpClient.SendAsync(new HttpRequestMessage(new HttpMethod("GET"),
                    $"{_usersSearchApiBaseUrl}{usersQuery}"));

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task UsersController_Get_FindUsersByDocumentId_LastLoginForClientNotSet()
        {
            var numberOfUsers = 10;
            var usersQuery = CreateUsersAndQuery(numberOfUsers, TestClientName, true);
            var response =
                await HttpClient.SendAsync(new HttpRequestMessage(new HttpMethod("GET"),
                    $"{_usersSearchApiBaseUrl}{usersQuery}"));

            var content = await response.Content.ReadAsStringAsync();

            var resource = (List<UserApiModel>) JsonConvert.DeserializeObject(content, typeof(List<UserApiModel>));
            Assert.Equal(numberOfUsers, resource.Count);
            Assert.Equal(numberOfUsers / 2, resource.Count(r => r.LastLoginDate.HasValue));
        }

        [Fact]
        public async Task UsersController_Get_FindUsersByDocumentId_NoDocumentIds_NotFound()
        {
            var numberOfUsers = 0;
            var usersQuery = CreateUsersAndQuery(numberOfUsers, TestClientName);
            var response =
                await HttpClient.SendAsync(new HttpRequestMessage(new HttpMethod("GET"),
                    $"{_usersSearchApiBaseUrl}{usersQuery}"));

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task UsersController_Get_FindUsersByDocumentId_NoClientId_BadRequest()
        {
            var numberOfUsers = 1;
            var usersQuery = CreateUsersAndQuery(numberOfUsers, string.Empty);
            var response = await HttpClient.SendAsync(new HttpRequestMessage(new HttpMethod("GET"), $"{_usersSearchApiBaseUrl}{usersQuery}"));

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task UsersController_Search_InvalidIdentityProvider_ReturnsBadRequest()
        {
            var response = await HttpClient.SendAsync(new HttpRequestMessage(new HttpMethod("GET"),
                $"{_identityProviderSearchBaseUrl}?searchText=john&identityProvider=Test"));
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public virtual async Task UsersController_Search_ReturnsUsers()
        {
            var logger = new Mock<ILogger>().Object;
            var settings = LdapTestHelper.GetLdapSettings();

            var testUsers = new List<Tuple<string, string>>
            {
                Tuple.Create("john", "smoltz"),
                Tuple.Create("john", "lackey"),
                Tuple.Create("johnny", "damon"),
                Tuple.Create("david", "wright")
            };

            var ldapConnectionProvider = new LdapConnectionProvider(settings, logger);
            var ldapEntries = LdapTestHelper.CreateTestUsers(testUsers, settings.BaseDn, ldapConnectionProvider);

            var response = await HttpClient.SendAsync(new HttpRequestMessage(new HttpMethod("GET"),
                $"{_identityProviderSearchBaseUrl}?searchText=john&identityProvider=Windows"));

            LdapTestHelper.RemoveEntries(ldapEntries, ldapConnectionProvider);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var content = await response.Content.ReadAsStringAsync();
            var users = JsonConvert.DeserializeObject<List<UserApiModel>>(content);
            Assert.NotNull(users);
            Assert.Equal(3, users.Count);
        }

        [Fact]
        public async Task UsersStore_FindByExternalProvider_ReturnsUser()
        {
            //add a user 
            var user = new User
            {
                SubjectId = $"{GetRandomString()}\\{GetRandomString()}",
                Username = GetRandomString(),
                ProviderName = FabricIdentityConstants.FabricExternalIdentityProviderTypes.Windows
            };

            CreateNewUser(user);

            //find them using user 
            var foundUser = await UserStore.FindByExternalProviderAsync(user.ProviderName, user.SubjectId);

            Assert.NotNull(foundUser);
        }
    }
}