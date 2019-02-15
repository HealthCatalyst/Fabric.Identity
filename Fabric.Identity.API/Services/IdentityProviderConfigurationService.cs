using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fabric.Identity.API.Configuration;
using IdentityServer4.Quickstart.UI;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;

namespace Fabric.Identity.API.Services
{
    public class IdentityProviderConfigurationService : IIdentityProviderConfigurationService
    {
        private readonly IAppConfiguration _appConfiguration;
        private readonly IAuthenticationSchemeProvider _schemeProvider;

        public IdentityProviderConfigurationService(IHttpContextAccessor httpContextAccessor, IAppConfiguration appConfiguration, IAuthenticationSchemeProvider schemeProvider)
        {
            _appConfiguration = appConfiguration ?? throw new ArgumentNullException(nameof(appConfiguration));
            _schemeProvider = schemeProvider ?? throw new ArgumentNullException(nameof(schemeProvider));
        }

        public async Task<ICollection<ExternalProvider>> GetConfiguredIdentityProviders()
        {
            var schemes = await _schemeProvider.GetAllSchemesAsync();

            var providers = schemes
                .Where(x => x.DisplayName != null && !AccountOptions.WindowsAuthenticationSchemeName.Equals(x.Name, StringComparison.OrdinalIgnoreCase))
                .Select(x => new ExternalProvider
                {
                    DisplayName = x.DisplayName,
                    AuthenticationScheme = x.Name
                }).ToList();

            if (_appConfiguration.WindowsAuthenticationEnabled)
            {
                // this is needed to handle windows auth schemes
                var windowsSchemes = schemes.Where(s => AccountOptions.WindowsAuthenticationSchemeName.Equals(s.Name, StringComparison.OrdinalIgnoreCase));
                if (windowsSchemes.Any())
                {
                    providers.Add(new ExternalProvider
                    {
                        AuthenticationScheme = AccountOptions.WindowsAuthenticationSchemeName,
                        DisplayName = "Windows"
                    });
                }
            }
            return providers;
        }
    }
}
