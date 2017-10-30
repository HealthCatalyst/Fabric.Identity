using System.Threading.Tasks;
using Fabric.Identity.API.Services;
using Fabric.Identity.API.Stores;
using IdentityServer4.Models;

namespace Fabric.Identity.API.DocumentDbStores
{
    public class DocumentDbClientStore : IClientManagementStore
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

        public void AddClient(Client client)
        {
            _documentDbService.AddDocument(client.ClientId, client);
        }

        public void UpdateClient(string clientId, Client client)
        {
            _documentDbService.UpdateDocument(clientId, client);
        }

        public void DeleteClient(string id)
        {
            _documentDbService.DeleteDocument<Client>(id);
        }
    }
}
