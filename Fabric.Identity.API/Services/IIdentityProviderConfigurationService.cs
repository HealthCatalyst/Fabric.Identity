using System.Collections.Generic;
using System.Threading.Tasks;
using Fabric.Identity.API.Models;

namespace Fabric.Identity.API.Services
{
    public interface IIdentityProviderConfigurationService
    {
        Task<ICollection<ExternalProvider>> GetConfiguredIdentityProviders();
    }
}
