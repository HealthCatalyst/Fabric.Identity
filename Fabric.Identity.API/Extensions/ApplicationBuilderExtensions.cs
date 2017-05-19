using System.Linq;
using Fabric.Identity.API.Configuration;
using IdentityServer4;
using Microsoft.AspNetCore.Builder;
using Microsoft.IdentityModel.Tokens;

namespace Fabric.Identity.API.Extensions
{
    public static class ApplicationBuilderExtensions
    {
        public static IApplicationBuilder UseExternalIdentityProviders(this IApplicationBuilder builder,
            IAppConfiguration appConfiguration)
        {
            if (appConfiguration.ExternalIdProviderSettings?.ExternalIdProviders == null
                || !appConfiguration.ExternalIdProviderSettings.ExternalIdProviders.Any()) return builder;

            foreach (var externalIdProvider in appConfiguration.ExternalIdProviderSettings.ExternalIdProviders)
            {
                var options = new OpenIdConnectOptions
                {
                    SignInScheme = IdentityServerConstants.ExternalCookieAuthenticationScheme,
                    SignOutScheme = IdentityServerConstants.SignoutScheme,

                    DisplayName = externalIdProvider.DisplayName,
                    Authority = externalIdProvider.Authority,
                    ClientId = externalIdProvider.ClientId,
                    ClientSecret = externalIdProvider.ClientSecret,
                    ResponseType = externalIdProvider.ResponseType,
                    GetClaimsFromUserInfoEndpoint = true,
                    TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = false
                    }
                };

                foreach (var s in externalIdProvider.Scope)
                {
                    options.Scope.Add(s);
                }

                builder.UseOpenIdConnectAuthentication(options);
            }
            return builder;
        }
    }
}
