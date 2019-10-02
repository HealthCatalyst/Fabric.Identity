using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using IdentityServer4.Models;
using IdentityServer4.Stores;

namespace Fabric.Identity.API.Persistence.CouchDb.Stores
{
    public class CouchDbResourceStore : IResourceStore
    {
        protected readonly IDocumentDbService DocumentDbService;

        public CouchDbResourceStore(IDocumentDbService documentDbService)
        {
            DocumentDbService = documentDbService;
        }

        public Task<IEnumerable<IdentityResource>> FindIdentityResourcesByScopeAsync(IEnumerable<string> scopeNames)
        {
            var identityResources = DocumentDbService
                .GetDocuments<IdentityResource>(FabricIdentityConstants.DocumentTypes.IdentityResourceDocumentType)
                .Result;

            var matchingResources = identityResources.Where(r => scopeNames.Contains(r.Name));

            return Task.FromResult(matchingResources);
        }

        public Task<IEnumerable<ApiResource>> FindApiResourcesByScopeAsync(IEnumerable<string> scopeNames)
        {
            var apiResources = DocumentDbService
                .GetDocuments<ApiResource>(FabricIdentityConstants.DocumentTypes.ApiResourceDocumentType).Result;

            var apiResourcesForScope = apiResources.Where(a => a.Scopes.Any(s => scopeNames.Contains(s.Name)));

            return Task.FromResult(apiResourcesForScope);
        }

        public Task<ApiResource> FindApiResourceAsync(string name)
        {
            return DocumentDbService.GetDocument<ApiResource>(name);
        }

        public Task<Resources> GetAllResourcesAsync()
        {
            var apiResources = DocumentDbService
                .GetDocuments<ApiResource>(FabricIdentityConstants.DocumentTypes.ApiResourceDocumentType).Result;
            var identityResources = DocumentDbService
                .GetDocuments<IdentityResource>(FabricIdentityConstants.DocumentTypes.IdentityResourceDocumentType)
                .Result;

            var result = new Resources(identityResources, apiResources);
            return Task.FromResult(result);
        }
    }
}