using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Castle.Components.DictionaryAdapter;
using Fabric.Identity.API;
using Fabric.Identity.API.DocumentDbStores;
using Fabric.Identity.API.Models;
using Fabric.Identity.API.Services;
using IdentityModel;
using Moq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Serilog;
using Xunit;

namespace Fabric.Identity.UnitTests
{
    public class InMemoryDocumentFixure
    {
        public InMemoryDocumentService DocumentService { get; } = new InMemoryDocumentService();

        public InMemoryDocumentFixure()
        {
            _users.ForEach(u => DocumentService.AddDocument($"{u.SubjectId}:{u.ProviderName}", u));
        }

        private readonly List<User> _users = new List<User>
        {
            new User
            {
                SubjectId = "UserOne",
                ProviderName = "AD",
                Username = "User One"
            },
            new User
            {
                SubjectId = "UserTwo",
                ProviderName = "AzureAD",
                Username = "User Two"
            },
            new User
            {
                SubjectId = "UserThree",
                ProviderName = "AzureAD",
                Username = "User Three"
            }
        };
    }

    public class UserStoreTests : IClassFixture<InMemoryDocumentFixure>
    {
        private readonly InMemoryDocumentFixure _fixture;

        public UserStoreTests(InMemoryDocumentFixure fixture)
        {
            _fixture = fixture;
        }

        [Theory, MemberData(nameof(SubjectIdData))]
        public void UserStore_CanFindBySubjectId(string subjectId, bool shouldBeFound)
        {
           var userStore = new DocumentDbUserStore(_fixture.DocumentService, new Mock<ILogger>().Object);

            var user = userStore.FindBySubjectId(subjectId).Result;
            if (user != null)
            {
                Console.WriteLine($"user subject id: {user.SubjectId}");
            }
            Assert.Equal(shouldBeFound, user != null);
        }

        [Theory, MemberData(nameof(ProviderSubjectIdData))]
        public void UserStore_CanFindByExternalProvider(string subjectId, string provider, bool shouldBeFound)
        {
            var userStore = new DocumentDbUserStore(_fixture.DocumentService, new Mock<ILogger>().Object);

            var user = userStore.FindByExternalProvider(provider, subjectId).Result;
            Assert.Equal(shouldBeFound, user != null);
        }

        [Fact]
        public async Task UserStore_CanSetLastLoginPerClient()
        {
            var clientId = "clientOne";
            var userStore = new DocumentDbUserStore(_fixture.DocumentService, new Mock<ILogger>().Object);

            var testUser = await userStore.FindBySubjectId("userone");
            await userStore.SetLastLogin(clientId, testUser.SubjectId);

            testUser = await userStore.FindBySubjectId(testUser.SubjectId);

            Assert.NotEmpty(testUser.LatestLoginsByClient);
            Assert.Equal(1, testUser.LatestLoginsByClient.Count);

            var login = testUser.LatestLoginsByClient.First();

            Assert.Equal(clientId, login.Key);
        }

        [Fact]
        public async Task UserStore_CanSetLoginForExistingClient()
        {
            var clientId = "clientTwo";
            var userStore = new DocumentDbUserStore(_fixture.DocumentService, new Mock<ILogger>().Object);
            var testUser = await userStore.FindBySubjectId("usertwo");
            await userStore.SetLastLogin(clientId, testUser.SubjectId);

            testUser = await userStore.FindBySubjectId(testUser.SubjectId);

            Assert.NotEmpty(testUser.LatestLoginsByClient);
            Assert.Equal(1, testUser.LatestLoginsByClient.Count);

            var login = testUser.LatestLoginsByClient.First();
            Assert.Equal(clientId, login.Key);
            
            await userStore.SetLastLogin(clientId, testUser.SubjectId);
            testUser = await userStore.FindBySubjectId(testUser.SubjectId);

            Assert.Equal(1, testUser.LatestLoginsByClient.Count);

            login = testUser.LatestLoginsByClient.First();
            Assert.Equal(clientId, login.Key);
        }

        [Fact]
        public void SerializeAndDeserializeClaim()
        {
            var jsonString = @"{
	        ""SubjectId"": ""FABRIC\\kyle.paul"",
            ""Username"": ""FABRIC\\kyle.paul"",
            ""ProviderName"": ""Windows"",
            ""Claims"": [{
                ""$id"": ""2"",
                ""Issuer"": ""LOCAL AUTHORITY"",
                ""OriginalIssuer"": ""LOCAL AUTHORITY"",
                ""Properties"": {
                },
                ""Subject"": {
                    ""AuthenticationType"": ""Negotiate"",
                    ""IsAuthenticated"": true,
                    ""Actor"": null,
                    ""BootstrapContext"": null,
                    ""Claims"": [{
                        ""$id"": ""3"",
                        ""Issuer"": ""LOCAL AUTHORITY"",
                        ""OriginalIssuer"": ""LOCAL AUTHORITY"",
                        ""Properties"": {


                        },
                        ""Type"": ""sub"",
                        ""Value"": ""FABRIC\\kyle.paul"",
                        ""ValueType"": ""http://www.w3.org/2001/XMLSchema#string""
                    },
                    {
                        ""$ref"": ""2""
                    },
                    {
                        ""$id"": ""4"",
                        ""Issuer"": ""LOCAL AUTHORITY"",
                        ""OriginalIssuer"": ""LOCAL AUTHORITY"",
                        ""Properties"": {
					
                        },
                        ""Type"": ""role"",
                        ""Value"": ""FABRIC\\Domain Users"",
                        ""ValueType"": ""http://www.w3.org/2001/XMLSchema#string""
                    },
                    {
                        ""$id"": ""5"",
                        ""Issuer"": ""LOCAL AUTHORITY"",
                        ""OriginalIssuer"": ""LOCAL AUTHORITY"",
                        ""Properties"": {
					
                        },
                        ""Type"": ""role"",
                        ""Value"": ""Everyone"",
                        ""ValueType"": ""http://www.w3.org/2001/XMLSchema#string""
                    },
                    {
                        ""$id"": ""6"",
                        ""Issuer"": ""LOCAL AUTHORITY"",
                        ""OriginalIssuer"": ""LOCAL AUTHORITY"",
                        ""Properties"": {
					
                        },
                        ""Type"": ""role"",
                        ""Value"": ""BUILTIN\\Users"",
                        ""ValueType"": ""http://www.w3.org/2001/XMLSchema#string""
                    },
                    {
                        ""$id"": ""7"",
                        ""Issuer"": ""LOCAL AUTHORITY"",
                        ""OriginalIssuer"": ""LOCAL AUTHORITY"",
                        ""Properties"": {
					
                        },
                        ""Type"": ""role"",
                        ""Value"": ""NT AUTHORITY\\NETWORK"",
                        ""ValueType"": ""http://www.w3.org/2001/XMLSchema#string""
                    },
                    {
                        ""$id"": ""8"",
                        ""Issuer"": ""LOCAL AUTHORITY"",
                        ""OriginalIssuer"": ""LOCAL AUTHORITY"",
                        ""Properties"": {
					
                        },
                        ""Type"": ""role"",
                        ""Value"": ""NT AUTHORITY\\Authenticated Users"",
                        ""ValueType"": ""http://www.w3.org/2001/XMLSchema#string""
                    },
                    {
                        ""$id"": ""9"",
                        ""Issuer"": ""LOCAL AUTHORITY"",
                        ""OriginalIssuer"": ""LOCAL AUTHORITY"",
                        ""Properties"": {
					
                        },
                        ""Type"": ""role"",
                        ""Value"": ""NT AUTHORITY\\This Organization"",
                        ""ValueType"": ""http://www.w3.org/2001/XMLSchema#string""
                    },
                    {
                        ""$id"": ""10"",
                        ""Issuer"": ""LOCAL AUTHORITY"",
                        ""OriginalIssuer"": ""LOCAL AUTHORITY"",
                        ""Properties"": {
					
                        },
                        ""Type"": ""role"",
                        ""Value"": ""NT AUTHORITY\\NTLM Authentication"",
                        ""ValueType"": ""http://www.w3.org/2001/XMLSchema#string""
                    }],
                    ""Label"": null,
                    ""Name"": null,
                    ""NameClaimType"": ""http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name"",
                    ""RoleClaimType"": ""http://schemas.microsoft.com/ws/2008/06/identity/claims/role""
                },
                ""Type"": ""name"",
                ""Value"": ""FABRIC\\kyle.paul"",
                ""ValueType"": ""http://www.w3.org/2001/XMLSchema#string""
            },
            {
                ""$ref"": ""4""
            },
            {
                ""$ref"": ""5""
            },
            {
                ""$ref"": ""6""
            },
            {
                ""$ref"": ""7""
            },
            {
                ""$ref"": ""8""
            },
            {
                ""$ref"": ""9""
            },
            {
                ""$ref"": ""10""
            }],
            ""LatestLoginsByClient"": {
		
            }
        }";
            var result = JsonConvert.DeserializeObject<User>(jsonString, new SerializationSettings().JsonSettings);

            Assert.NotNull(result);
        }        

        public static IEnumerable<object[]> SubjectIdData => new[]
        {
            new object[]{ "userone", true},
            new object[]{ "usertwo", true},
            new object[]{ "foo", false}
        };

        public static IEnumerable<object[]> ProviderSubjectIdData => new[]
        {
            new object[]{ "userone", "ad", true},
            new object[]{ "usertwo", "azuread", true},
            new object[]{ "userthree", "ad", false},
            new object[]{ "foo", "" , false}
        };
    }
}
