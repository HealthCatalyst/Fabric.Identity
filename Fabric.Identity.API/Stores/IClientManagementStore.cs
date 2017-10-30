using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using IdentityServer4.Stores;
using IdentityServer4.Models;

namespace Fabric.Identity.API.Stores
{
    public interface IClientManagementStore : IClientStore
    {
        void AddClient(Client client);
        void UpdateClient(string clientId, Client client);
        void DeleteClient(string id);
    }
}
