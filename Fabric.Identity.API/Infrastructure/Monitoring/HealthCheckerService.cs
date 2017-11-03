using System;
using System.Threading.Tasks;
using Fabric.Identity.API.Persistence;

namespace Fabric.Identity.API.Infrastructure.Monitoring
{
    public class HealthCheckerService : IHealthCheckerService
    {
        private readonly IClientManagementStore _clientManagementStore;

        public HealthCheckerService(IClientManagementStore clientManagementStore)
        {
            _clientManagementStore = clientManagementStore
                                     ?? throw new ArgumentNullException(nameof(clientManagementStore));
        }

        public Task<bool> CheckHealth()
        {
            return Task.FromResult(_clientManagementStore.GetClientCount() >= 0);
        }
    }
}