using Fabric.Identity.API.Services;
using IdentityServer4.Models;

namespace Fabric.Identity.API.Stores.Document
{
    public class DocumentDbApiResourceStore : BaseResourceStore, IApiResourceStore
    {
        public DocumentDbApiResourceStore(IDocumentDbService documentDbService) : base(documentDbService)
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