using System;
using System.Collections.Generic;
using System.Text;
using Castle.Components.DictionaryAdapter;
using Fabric.Identity.API.DocumentDbStores;
using Fabric.Identity.API.Models;
using Fabric.Identity.API.Services;
using Moq;
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
           var userStore = new DocumentDbUserStore(_fixture.DocumentService, null);

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
            var userStore = new DocumentDbUserStore(_fixture.DocumentService, null);

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
            new object[]{ "foo", "" , false},
        };
    }
}
