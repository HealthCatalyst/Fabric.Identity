using IdentityServer4.Models;
using IdentityServer4.Stores;

namespace Fabric.Identity.API.Persistence
{
    public interface IClientManagementStore : IClientStore
    {
        int GetClientCount();
        void AddClient(Client client);
        void UpdateClient(string clientId, Client client);
        void DeleteClient(string id);
    }
}