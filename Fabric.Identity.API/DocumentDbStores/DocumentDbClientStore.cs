using System.Threading.Tasks;
using Fabric.Identity.API.CouchDb;
using Fabric.Identity.API.Services;
using IdentityServer4.Models;
using IdentityServer4.Stores;

namespace Fabric.Identity.API.DocumentDbStores
{
    public class DocumentDbClientStore : IClientStore
    {
        private readonly IDocumentDbService _documentDbService;

        public DocumentDbClientStore(IDocumentDbService documentDbService)
        {
            _documentDbService = documentDbService;
        }

        public Task<Client> FindClientByIdAsync(string clientId)
        {
            return _documentDbService.GetDocument<Client>(clientId);
        }
    }
}
