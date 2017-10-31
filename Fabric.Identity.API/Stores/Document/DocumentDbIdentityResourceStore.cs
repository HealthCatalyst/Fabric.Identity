using Fabric.Identity.API.Services;
using IdentityServer4.Models;

namespace Fabric.Identity.API.Stores.Document
{
    public class DocumentDbIdentityResourceStore : BaseResourceStore, IIdentityResourceStore
    {
        public DocumentDbIdentityResourceStore(IDocumentDbService documentDbService) : base(documentDbService)
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