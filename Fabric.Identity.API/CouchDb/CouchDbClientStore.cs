using System.Threading.Tasks;
using IdentityServer4.Models;
using IdentityServer4.Stores;

namespace Fabric.Identity.API.CouchDb
{
    public class CouchDbClientStore : IClientStore
    {
        private readonly IDocumentDbService _documentDbService;

        public CouchDbClientStore(IDocumentDbService documentDbService)
        {
            _documentDbService = documentDbService;
        }

        public Task<Client> FindClientByIdAsync(string clientId)
        {
            return _documentDbService.GetDocument<Client>(clientId);
        }
    }
}
