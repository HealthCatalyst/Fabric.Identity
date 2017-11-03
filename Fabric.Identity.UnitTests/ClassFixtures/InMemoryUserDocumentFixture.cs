using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fabric.Identity.API;
using Fabric.Identity.API.Models;
using Fabric.Identity.API.Persistence;
using Fabric.Identity.API.Persistence.InMemory.Services;
using Moq;

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
            var mockDocumentDbService = new Mock<IDocumentDbService>();
            mockDocumentDbService
                .Setup(documentDbService => documentDbService.GetDocuments<User>(It.IsAny<string>()))
                .Returns((string subjectId) =>
                {
                    return Task.FromResult(_users.Where(
                        u =>
                            $"{FabricIdentityConstants.DocumentTypes.UserDocumentType.ToLower()}{u.SubjectId.ToLower()}" ==
                            subjectId.ToLower()
                            ||
                            $"{FabricIdentityConstants.DocumentTypes.UserDocumentType.ToLower()}{u.SubjectId.ToLower()}:{u.ProviderName.ToLower()}" ==
                            subjectId.ToLower()));
                });
            DocumentService = mockDocumentDbService.Object;
        }

        public IDocumentDbService DocumentService { get; }
    }
}
