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

        public IdentityProviderConfigurationService(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
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

            if (AccountOptions.WindowsAuthenticationEnabled)
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
