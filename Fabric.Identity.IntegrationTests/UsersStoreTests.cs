using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Fabric.Identity.API.DocumentDbStores;
using Fabric.Identity.API.Models;
using Moq;
using Newtonsoft.Json;
using Serilog;
using Xunit;

namespace Fabric.Identity.IntegrationTests
{
    public class UsersStoreTests : IntegrationTestsFixture
    {     
        private readonly ILogger _logger = new Mock<ILogger>().Object;
        private readonly DocumentDbUserStore _documentDbUserStore;
        private readonly string _usersSearchApiBaseUrl = "/api/users";


        public UsersStoreTests() : base(false)
        {          
            var documentDbService = CouchDbService;
            _documentDbUserStore = new DocumentDbUserStore(documentDbService, _logger);
        }

        private void CreateNewUser(User user)
        {
           _documentDbUserStore.AddUser(user);
        }

        private static readonly Random Random = new Random();
        private static string GetRandomString()
        {
            string path = Path.GetRandomFileName();
            path = path.Replace(".", "");

            var stringLength = Random.Next(5, path.Length);
            
            return path.Substring(0, stringLength);
        }

        private string CreateUsersAndQuery(int numberToCreate, string clientId, bool halfWithoutLoginForClient = false)
        {
            var queryBuilder = new StringBuilder($"?clientId={clientId}");

            for (int i = 1; i <= numberToCreate; i++)
            {
                var user = new User
                {
                    SubjectId = $"{GetRandomString()}\\{GetRandomString()}",
                    Username = i.ToString(),
                    ProviderName = "Windows"
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
                queryBuilder.Append($"&userIds={user.SubjectId}:{user.ProviderName}");
            }

            return queryBuilder.ToString();
        }

        [Fact]
        public async Task UsersController_Get_FindsUsersByDocumentId_LastLoginForClientSet()
        {
            var numberOfUsers = 10;
            var usersQuery = CreateUsersAndQuery(numberOfUsers, TestClientName);      
            var response = await this.HttpClient.SendAsync(new HttpRequestMessage(new HttpMethod("GET"), $"{_usersSearchApiBaseUrl}{usersQuery}"));

            var content = await response.Content.ReadAsStringAsync();
            
            var resource = (List<UserApiModel>)JsonConvert.DeserializeObject(content, typeof(List<UserApiModel>));
            Assert.Equal(numberOfUsers, resource.Count);
            foreach (var userApiModel in resource)
            {
                Assert.True(userApiModel.LastLoginDate.HasValue);
            }
        }

        [Fact]
        public async Task UsersController_Get_FindUsersByDocumentId_LastLoginForClientNotSet()
        {
            var numberOfUsers = 10;
            var usersQuery = CreateUsersAndQuery(numberOfUsers, TestClientName, true);
            var response = await this.HttpClient.SendAsync(new HttpRequestMessage(new HttpMethod("GET"), $"{_usersSearchApiBaseUrl}{usersQuery}"));

            var content = await response.Content.ReadAsStringAsync();

            var resource = (List<UserApiModel>)JsonConvert.DeserializeObject(content, typeof(List<UserApiModel>));
            Assert.Equal(numberOfUsers, resource.Count);
            Assert.Equal(numberOfUsers/2, resource.Count(r => r.LastLoginDate.HasValue));
        }

        [Fact]
        public async Task UsersController_Get_FindUsersByDocumentId_InvalidClientId_BadRequest()
        {
            var numberOfUsers = 1;
            var usersQuery = CreateUsersAndQuery(numberOfUsers, "foo");
            var response = await this.HttpClient.SendAsync(new HttpRequestMessage(new HttpMethod("GET"), $"{_usersSearchApiBaseUrl}{usersQuery}"));

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task UsersController_Get_FindUsersByDocumentId_NoDocumentIds_NotFound()
        {
            var numberOfUsers = 0;
            var usersQuery = CreateUsersAndQuery(numberOfUsers, TestClientName);
            var response = await this.HttpClient.SendAsync(new HttpRequestMessage(new HttpMethod("GET"), $"{_usersSearchApiBaseUrl}{usersQuery}"));

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task UsersStore_FindByExternalProvider_ReturnsUser()
        {
            //add a user 
            var user = new User
            {
                SubjectId = $"{GetRandomString()}\\{GetRandomString()}",
                Username = GetRandomString(),
                ProviderName = "Windows"
            };

            CreateNewUser(user);

            //find them using user 
            var foundUser = await _documentDbUserStore.FindByExternalProvider(user.ProviderName, user.SubjectId);

            Assert.NotNull(foundUser);
        }
    }
}
