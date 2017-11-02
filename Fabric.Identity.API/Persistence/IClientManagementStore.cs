using System.Threading.Tasks;
using System.Collections.Generic;
using IdentityServer4.Models;
using IdentityServer4.Stores;

namespace Fabric.Identity.API.Persistence
{
    public interface IClientManagementStore : IClientStore
    {
        IEnumerable<Client> GetAllClients();
        int GetClientCount();
        void AddClient(Client client);
        void UpdateClient(string clientId, Client client);
        void DeleteClient(string id);


        Task AddClientAsync(Client client);
        Task UpdateClientAsync(string clientId, Client client);
        Task DeleteClientAsync(string id);

    }
}