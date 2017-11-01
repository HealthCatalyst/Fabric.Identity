using Fabric.Identity.API.Persistence.CouchDb.Stores;
using IdentityServer4.Models;

namespace Fabric.Identity.API.Persistence.InMemory.Stores
{
    public class InMemoryIdentityResourceStore : BaseResourceStore, IIdentityResourceStore
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
    }
}