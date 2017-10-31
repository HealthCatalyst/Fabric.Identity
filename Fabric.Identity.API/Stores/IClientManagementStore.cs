using IdentityServer4.Models;
using IdentityServer4.Stores;

namespace Fabric.Identity.API.Stores
{
    public interface IClientManagementStore : IClientStore
    {
        void AddClient(Client client);
        void UpdateClient(string clientId, Client client);
        void DeleteClient(string id);
    }
}