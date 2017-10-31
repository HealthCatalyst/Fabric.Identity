using System.Collections.Generic;
using Fabric.Identity.API.Models;
using Fabric.Identity.API.Services.Databases.Document;

namespace Fabric.Identity.UnitTests.ClassFixtures
{
    public class InMemoryUserDocumentFixture
    {
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

        public InMemoryUserDocumentFixture()
        {
            DocumentService = new InMemoryDocumentService();
            _users.ForEach(u => DocumentService.AddDocument($"{u.SubjectId}:{u.ProviderName}", u));
        }

        public InMemoryDocumentService DocumentService { get; }

        public void Dispose()
        {
            DocumentService.Clean();
        }
    }
}
