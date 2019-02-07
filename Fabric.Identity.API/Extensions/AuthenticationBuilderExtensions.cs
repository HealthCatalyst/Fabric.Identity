using System.Linq;
using Fabric.Identity.API.Configuration;
using IdentityServer4;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;

namespace Fabric.Identity.API.Extensions
{
    public static class AuthenticationBuilderExtensions
    {
        public static AuthenticationBuilder AddAzureIdentityProviderIfApplicable(this AuthenticationBuilder builder,
            IAppConfiguration appConfiguration)
        {
            if (!appConfiguration.AzureAuthenticationEnabled)
            {
                return builder;
            }

            builder.AddOpenIdConnect(FabricIdentityConstants.AuthenticationSchemes.Azure, appConfiguration.AzureActiveDirectorySettings.DisplayName,
                options =>
                {
                    options.SignInScheme = IdentityServerConstants.ExternalCookieAuthenticationScheme;
                    options.SignOutScheme = IdentityServerConstants.SignoutScheme;

                    options.Authority = appConfiguration.AzureActiveDirectorySettings.Authority;
                    options.ResponseType = OpenIdConnectResponseType.CodeIdToken;
                    options.ClaimsIssuer = appConfiguration.AzureActiveDirectorySettings.ClaimsIssuer;
                    options.ClientId = appConfiguration.AzureActiveDirectorySettings.ClientId;
                    options.ClientSecret = appConfiguration.AzureActiveDirectorySettings.ClientSecret;
                    options.GetClaimsFromUserInfoEndpoint = true;
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = false
                    };
                    foreach (var s in appConfiguration.AzureActiveDirectorySettings.Scope)
                    {
                        options.Scope.Add(s);
                    }
                });

            return builder;
        }

        public static AuthenticationBuilder AddExternalIdentityProviders(this AuthenticationBuilder builder,
            IAppConfiguration appConfiguration)
        {
            if (appConfiguration.ExternalIdProviderSettings?.ExternalIdProviders?.Any() != true)
            {
                return builder;
            }
            // Add OpenIdConnect options to each provider.
            foreach (var externalIdProvider in appConfiguration.ExternalIdProviderSettings.ExternalIdProviders)
            {
                builder.AddOpenIdConnect("oidc", externalIdProvider.DisplayName, options =>
                {
                    options.SignInScheme = IdentityServerConstants.ExternalCookieAuthenticationScheme;
                    options.SignOutScheme = IdentityServerConstants.SignoutScheme;
                    options.Authority = externalIdProvider.Authority;
                    options.ClientId = externalIdProvider.ClientId;
                    options.ClientSecret = externalIdProvider.ClientSecret;
                    options.ResponseType = externalIdProvider.ResponseType;
                    options.GetClaimsFromUserInfoEndpoint = true;
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = false
                    };
                    foreach (var s in externalIdProvider.Scope)
                    {
                        options.Scope.Add(s);
                    }
                });
            }

            return builder;
        }
    }
}
