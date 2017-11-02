using System.Threading.Tasks;
using Fabric.Identity.API.Persistence.CouchDb.Stores;
using IdentityServer4.Models;

namespace Fabric.Identity.API.Persistence.InMemory.Stores
{
    public class InMemoryIdentityResourceStore : CouchDbResourceStore, IIdentityResourceStore
    {
        public InMemoryIdentityResourceStore(IDocumentDbService documentDbService) : base(documentDbService)
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