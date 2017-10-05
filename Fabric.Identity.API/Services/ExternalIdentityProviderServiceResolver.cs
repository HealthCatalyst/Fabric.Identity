using System;
using Microsoft.Extensions.DependencyInjection;

namespace Fabric.Identity.API.Services
{
    public class ExternalIdentityProviderServiceResolver : IExternalIdentityProviderServiceResolver
    {
        private readonly IServiceProvider _serviceProvider;
        public ExternalIdentityProviderServiceResolver(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        }
        public IExternalIdentityProviderService GetExternalIdentityProviderService(string identityProviderName)
        {
            switch (identityProviderName)
            {
                case FabricIdentityConstants.FabricExternalIdentityProviderTypes.Windows:
                    return _serviceProvider.GetRequiredService<LdapProviderService>();
                default:
                    throw new InvalidOperationException("There is no search provider specified for the requested Identity Provider.");
            }
        }
    }
}
