using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using IdentityServer4.Quickstart.UI;

namespace Fabric.Identity.API.Services
{
    public interface IIdentityProviderConfigurationService
    {
        ICollection<ExternalProvider> GetConfiguredIdentityProviders();
    }
}
