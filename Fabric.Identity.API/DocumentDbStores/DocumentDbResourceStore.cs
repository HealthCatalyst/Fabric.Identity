using Fabric.Identity.API.Services;
using IdentityServer4.Models;
using IdentityServer4.Stores;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Fabric.Identity.API.DocumentDbStores
{
    public class DocumentDbResourceStore : IResourceStore
    {
        private readonly IDocumentDbService _documentDbService;

        public DocumentDbResourceStore(IDocumentDbService documentDbService)
        {
            _documentDbService = documentDbService;
        }

        public Task<IEnumerable<IdentityResource>> FindIdentityResourcesByScopeAsync(IEnumerable<string> scopeNames)
        {
            var identityResources = _documentDbService.GetDocuments<IdentityResource>(FabricIdentityConstants.DocumentTypes.IdentityResourceDocumentType).Result;

            var matchingResources = identityResources.Where(r => scopeNames.Contains(r.Name));

            return Task.FromResult(matchingResources);
        }

        public Task<IEnumerable<ApiResource>> FindApiResourcesByScopeAsync(IEnumerable<string> scopeNames)
        {
            var apiResources = _documentDbService.GetDocuments<ApiResource>(FabricIdentityConstants.DocumentTypes.ApiResourceDocumentType).Result;

            var apiResourcesForScope = apiResources.Where(a => a.Scopes.Any(s => scopeNames.Contains(s.Name)));

            return Task.FromResult(apiResourcesForScope);
        }

        public Task<ApiResource> FindApiResourceAsync(string name)
        {
            return _documentDbService.GetDocument<ApiResource>(name);
        }

        public Task<Resources> GetAllResources()
        {
            var apiResources = _documentDbService.GetDocuments<ApiResource>(FabricIdentityConstants.DocumentTypes.ApiResourceDocumentType).Result;
            var identityResources = _documentDbService.GetDocuments<IdentityResource>(FabricIdentityConstants.DocumentTypes.IdentityResourceDocumentType).Result;

            var result = new Resources(identityResources, apiResources);
            return Task.FromResult(result);
        }
    }
}
