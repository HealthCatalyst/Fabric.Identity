using IdentityServer4.Models;

namespace Fabric.Identity.API.Persistence.InMemory.Stores
{
    public class InMemoryApiResourceStore : InMemoryResourceStore, IApiResourceStore
    {
        public InMemoryApiResourceStore(IDocumentDbService documentDbService) : base(documentDbService)
        {
        }

        public ApiResource GetResource(string id)
        {
            return DocumentDbService.GetDocument<ApiResource>(id).Result;
        }

        public void AddResource(ApiResource apiResource)
        {
            DocumentDbService.AddDocument(apiResource.Name, apiResource);
        }

        public void UpdateResource(string id, ApiResource apiResource)
        {
            DocumentDbService.UpdateDocument(id, apiResource);
        }

        public void DeleteResource(string id)
        {
            DocumentDbService.DeleteDocument<ApiResource>(id);
        }
    }
}