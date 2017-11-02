using System.Threading.Tasks;
using IdentityServer4.Models;
using IdentityServer4.Stores;

namespace Fabric.Identity.API.Persistence.InMemory.Stores
{
    public class InMemoryClientManagementStore : IClientManagementStore
    {
        private readonly IClientStore _clientStore;
        private readonly IDocumentDbService _documentDbService;

        public InMemoryClientManagementStore(IClientStore innerClientStore, IDocumentDbService documentDbService)
        {
            _clientStore = innerClientStore;
            _documentDbService = documentDbService;
        }

        public Task<Client> FindClientByIdAsync(string clientId)
        {
            return _clientStore.FindClientByIdAsync(clientId);
        }

        public int GetClientCount()
        {
            return _documentDbService.GetDocumentCount(FabricIdentityConstants.DocumentTypes.ClientDocumentType)
                .Result;
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