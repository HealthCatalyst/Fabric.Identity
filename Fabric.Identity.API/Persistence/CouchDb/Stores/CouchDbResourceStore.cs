namespace Fabric.Identity.API.Persistence.CouchDb.Stores
{
    public class CouchDbResourceStore : BaseResourceStore
    {
        public CouchDbResourceStore(IDocumentDbService documentDbService) : base(documentDbService)
        {
        }
    }
}