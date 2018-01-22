using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fabric.Identity.API.Configuration;
using Fabric.Identity.API.Models;
using IdentityServer4.Quickstart.UI;
using Microsoft.AspNetCore.Http;

namespace Fabric.Identity.API.Services
{
    public class IdentityProviderConfigurationService : IIdentityProviderConfigurationService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IAppConfiguration _appConfiguration;

        public IdentityProviderConfigurationService(IHttpContextAccessor httpContextAccessor, IAppConfiguration appConfiguration)
        {
            _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
            _appConfiguration = appConfiguration ?? throw new ArgumentNullException(nameof(appConfiguration));
        }

        public ICollection<ExternalProvider> GetConfiguredIdentityProviders()
        {
            var schemes = _httpContextAccessor.HttpContext.Authentication.GetAuthenticationSchemes();

            var providers = schemes
                .Where(x => x.DisplayName != null && !AccountOptions.WindowsAuthenticationSchemes.Contains(x.AuthenticationScheme))
                .Select(x => new ExternalProvider
                {
                    DisplayName = x.DisplayName,
                    AuthenticationScheme = x.AuthenticationScheme
                }).ToList();

            if (_appConfiguration.WindowsAuthenticationEnabled)
            {
                // this is needed to handle windows auth schemes
                var windowsSchemes = schemes.Where(s => AccountOptions.WindowsAuthenticationSchemes.Contains(s.AuthenticationScheme));
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
