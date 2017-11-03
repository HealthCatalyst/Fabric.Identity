using System.Threading.Tasks;
using IdentityServer4.Models;

namespace Fabric.Identity.API.Persistence.CouchDb.Stores
{
    public class CouchDbIdentityResourceStore : CouchDbResourceStore, IIdentityResourceStore
    {
        public CouchDbIdentityResourceStore(IDocumentDbService documentDbService) : base(documentDbService)
        {
        }

        public IdentityResource GetResource(string id)
        {
            return DocumentDbService.GetDocument<IdentityResource>(id).Result;
        }

        public void AddResource(IdentityResource apiResource)
        {
            DocumentDbService.AddDocument(apiResource.Name, apiResource);
        }

        public void UpdateResource(string id, IdentityResource apiResource)
        {
            DocumentDbService.UpdateDocument(id, apiResource);
        }

        public void DeleteResource(string id)
        {
            DocumentDbService.DeleteDocument<IdentityResource>(id);
        }

        public Task AddResourceAsync(IdentityResource resource)
        {
            throw new System.NotImplementedException();
        }

        public Task UpdateResourceAsync(string id, IdentityResource resource)
        {
            throw new System.NotImplementedException();
        }

        public Task<IdentityResource> GetResourceAsync(string id)
        {
            throw new System.NotImplementedException();
        }

        public Task DeleteResourceAsync(string id)
        {
            throw new System.NotImplementedException();
        }
    }
}