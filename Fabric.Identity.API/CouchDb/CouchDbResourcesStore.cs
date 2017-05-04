using System.Collections.Generic;
using System.Threading.Tasks;
using IdentityServer4.Models;
using IdentityServer4.Stores;

namespace Fabric.Identity.API.CouchDb
{
    public class CouchDbResourcesStore : IResourceStore
    {
        private readonly IDocumentDbService _documentDbService;

        public CouchDbResourcesStore(IDocumentDbService documentDbService)
        {
            _documentDbService = documentDbService;
        }

        public Task<IEnumerable<IdentityResource>> FindIdentityResourcesByScopeAsync(IEnumerable<string> scopeNames)
        {
            return _documentDbService.FindDocumentsByKeys<IdentityResource>("identityresource", scopeNames);
        }

        public Task<IEnumerable<ApiResource>> FindApiResourcesByScopeAsync(IEnumerable<string> scopeNames)
        {
            return _documentDbService.FindDocumentsByKeys<ApiResource>("apiresource",scopeNames);
        }

        public Task<ApiResource> FindApiResourceAsync(string name)
        {
            return _documentDbService.GetDocument<ApiResource>(name);
        }

        public Task<Resources> GetAllResources()
        {
            var apiResources = _documentDbService.FindDocuments<ApiResource>("apiresource").Result;
            var identityResources = _documentDbService.FindDocuments<IdentityResource>("identityresource").Result;

            var result = new Resources(identityResources, apiResources);
            return Task.FromResult(result);
        }
    }
}
