using System.Collections.Generic;
using System.Threading.Tasks;
using IdentityServer4.Quickstart.UI;

namespace Fabric.Identity.API.Services
{
    public interface IIdentityProviderConfigurationService
    {
        Task<ICollection<ExternalProvider>> GetConfiguredIdentityProviders();
    }
}
