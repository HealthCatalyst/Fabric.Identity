using System.Collections.Generic;
using Fabric.Identity.API.Models;

namespace Fabric.Identity.API.Services
{
    public interface IIdentityProviderConfigurationService
    {
        ICollection<ExternalProvider> GetConfiguredIdentityProviders();
    }
}
