using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fabric.Identity.API.Services;
using IdentityServer4.Models;
using Moq;
using Xunit;
using Fabric.Identity.API.Stores.Document;

namespace Fabric.Identity.UnitTests
{
    public class ResourceStoreTests
    {
        private readonly List<ApiResource> _apiResources = new List<ApiResource>
        {
            new ApiResource
            {
                Name = "apiresource:foo",
                DisplayName = "foo",
                Scopes = new List<Scope>
                {
                    new Scope
                    {
                        Name = "api1"
                    }
                }
            },
            new ApiResource
            {
                Name = "apiresource:bar",
                DisplayName = "bar",
                Scopes = new List<Scope>
                {
                    new Scope
                    {
                        Name = "api2"
                    }
                }
            },
            new ApiResource
            {
                Name = "apiresource:jam",
                DisplayName = "jam",
                Scopes = new List<Scope>
                {
                    new Scope
                    {
                        Name = "api3"
                    }
                }
            }
        };

        private readonly List<IdentityResource> _identityResources = new List<IdentityResource>
        {
            new IdentityResource
            {
                Name = "identityresource:foo",
                DisplayName = "foo"
            },
            new IdentityResource
            {
                Name = "identityresource:bar",
                DisplayName = "bar"
            },
            new IdentityResource
            {
                Name = "identityresource:jam",
                DisplayName = "jam"
            }
        };

        public static IEnumerable<object[]> IdentityResourceScopeData => new[]
        {
            new object[] {new List<string> {"identityresource:foo"}, 1},
            new object[] {new List<string> {"identityresource:bar"}, 1},
            new object[] {new List<string> {"identityresource:foo", "identityresource:bar"}, 2},
            new object[] {new List<string> {"identityresource:foo", "identityresource:bar", "identityresource:jam"}, 3}
        };

        public static IEnumerable<object[]> ApiResourceScopeData => new[]
        {
            new object[] {new List<string> {"api1"}, 1},
            new object[] {new List<string> {"api2"}, 1},
            new object[] {new List<string> {"api1", "api2"}, 2},
            new object[] {new List<string> {"api1", "api2", "api3"}, 3}
        };

        [Theory]
        [MemberData(nameof(IdentityResourceScopeData))]
        public void DocumentDbStore_CanFindIdentityResourceByScope(IEnumerable<string> scopeNames,
            int expectedResultCount)
        {
            var mockDbService = new Mock<IDocumentDbService>()
                .SetupGetDocument(_identityResources)
                .Create();

            var documentDbResourceStore = new DocumentDbResourceStore(mockDbService);

            var identityResources = documentDbResourceStore.FindIdentityResourcesByScopeAsync(scopeNames).Result;
            Assert.Equal(expectedResultCount, identityResources.Count());
        }

        [Theory]
        [MemberData(nameof(ApiResourceScopeData))]
        public void DocumentDbStore_CanFindApiResourceByScope(IEnumerable<string> scopeNames, int expectedResultCount)
        {
            var mockDbService = new Mock<IDocumentDbService>()
                .SetupGetDocument(_apiResources)
                .Create();

            var documentDbResourceStore = new DocumentDbResourceStore(mockDbService);

            var apiResources = documentDbResourceStore.FindApiResourcesByScopeAsync(scopeNames).Result;
            Assert.Equal(expectedResultCount, apiResources.Count());
        }
    }

    public static class DocumentDbServiceExtensions
    {
        public static Mock<IDocumentDbService> SetupGetDocument(
            this Mock<IDocumentDbService> mockIdentityResourceService,
            List<IdentityResource> identityResources)
        {
            mockIdentityResourceService.Setup(service => service.GetDocuments<IdentityResource>(It.IsAny<string>()))
                .Returns((string documentType) =>
                {
                    return Task.FromResult(identityResources.Where(id => id.Name.StartsWith("identityresource:")));
                });

            return mockIdentityResourceService;
        }

        public static IDocumentDbService Create(this Mock<IDocumentDbService> mockIdentityResourceService)
        {
            return mockIdentityResourceService.Object;
        }

        public static Mock<IDocumentDbService> SetupGetDocument(
            this Mock<IDocumentDbService> mockIdentityResourceService,
            List<ApiResource> apiResources)
        {
            mockIdentityResourceService.Setup(service => service.GetDocuments<ApiResource>(It.IsAny<string>()))
                .Returns((string documentType) =>
                {
                    return Task.FromResult(apiResources.Where(id => id.Name.StartsWith("apiresource:")));
                });

            return mockIdentityResourceService;
        }
    }
}