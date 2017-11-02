using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using IdentityServer4.Models;

namespace Fabric.Identity.API.Persistence.CouchDb.Stores
{
    public class CouchDbClientStore : IClientManagementStore
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

        public IEnumerable<Client> GetAllClients()
        {
            return _documentDbService.GetDocuments<Client>(FabricIdentityConstants.DocumentTypes.ClientDocumentType)
                .Result.ToList();
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