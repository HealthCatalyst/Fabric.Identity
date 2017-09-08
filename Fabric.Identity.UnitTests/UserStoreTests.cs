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
using Fabric.Identity.UnitTests.ClassFixtures;
using IdentityModel;
using Moq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Serilog;
using Xunit;

namespace Fabric.Identity.UnitTests
{
    
    public class UserStoreTests : IClassFixture<InMemoryUserDocumentFixture>
    {
        private readonly InMemoryUserDocumentFixture _fixture;

        public UserStoreTests(InMemoryUserDocumentFixture fixture)
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
