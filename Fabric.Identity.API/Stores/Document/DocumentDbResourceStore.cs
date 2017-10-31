using Fabric.Identity.API.Services;

namespace Fabric.Identity.API.Stores.Document
{
    public class DocumentDbResourceStore : BaseResourceStore
    {
        public DocumentDbResourceStore(IDocumentDbService documentDbService) : base(documentDbService)
        {
        }
    }
}