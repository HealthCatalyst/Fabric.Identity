using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fabric.Identity.API.Configuration;
using Fabric.Identity.API.Models;
using IdentityServer4.Quickstart.UI;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;

namespace Fabric.Identity.API.Services
{
    public class IdentityProviderConfigurationService : IIdentityProviderConfigurationService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IAppConfiguration _appConfiguration;
        private readonly IAuthenticationSchemeProvider _authenticationSchemeProvider;

        public IdentityProviderConfigurationService(IHttpContextAccessor httpContextAccessor, IAuthenticationSchemeProvider authenticationSchemeProvider, IAppConfiguration appConfiguration)
        {
            _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
            _appConfiguration = appConfiguration ?? throw new ArgumentNullException(nameof(appConfiguration));
            _authenticationSchemeProvider = authenticationSchemeProvider ??
                                            throw new ArgumentNullException(nameof(httpContextAccessor));
        }

        public async Task<ICollection<ExternalProvider>> GetConfiguredIdentityProviders()
        {
            var schemes = await _authenticationSchemeProvider.GetAllSchemesAsync();

            var providers = schemes
                .Where(x => (x.DisplayName != null || (x.DisplayName == null && x.Name == "Windows")) && !AccountOptions.WindowsAuthenticationSchemes.Contains(x.Name))
                .Select(x => new ExternalProvider
                {
                    DisplayName = x.DisplayName?? x.Name,
                    AuthenticationScheme = x.Name
                }).ToList();

            if (_appConfiguration.WindowsAuthenticationEnabled)
            {
                // this is needed to handle windows auth schemes
                var windowsSchemes = schemes.Where(s => AccountOptions.WindowsAuthenticationSchemes.Contains(s.Name));
                if (windowsSchemes.Any())
                {
                    providers.Add(new ExternalProvider
                    {
                        AuthenticationScheme = AccountOptions.WindowsAuthenticationSchemes.First(),
                        DisplayName = AccountOptions.WindowsAuthenticationDisplayName
                    });
                }
            }
            return providers;
        }
    }
}
