using System.Linq;
using System.Threading.Tasks;
using Fabric.Identity.API.Persistence;
using IdentityServer4.Services;

namespace Fabric.Identity.API.Services
{
    public class CorsPolicyService : ICorsPolicyService
    {
        private readonly IClientManagementStore _clientManagementStore;

        public CorsPolicyService(IClientManagementStore clientManagementStore)
        {
            _clientManagementStore = clientManagementStore;
        }

        public Task<bool> IsOriginAllowedAsync(string origin)
        {
            var clients = _clientManagementStore.GetAllClients();
            return Task.FromResult(clients != null && clients.SelectMany(c => c.AllowedCorsOrigins).Contains(origin));
        }
    }
}