using System;
using System.IO;
using System.Linq;
using Fabric.Identity.API.Configuration;
using IdentityServer4;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using Serilog;

namespace Fabric.Identity.API.Extensions
{
    using Microsoft.IdentityModel.Protocols.OpenIdConnect;

    public static class AuthenticationBuilderExtensions
    {
        public static AuthenticationBuilder AddAzureIdentityProviderIfApplicable(this AuthenticationBuilder builder,
                                                                       IAppConfiguration appConfiguration)
        {
            if (!appConfiguration.AzureAuthenticationEnabled)
            {
                return builder;
            }

            return builder.AddOpenIdConnect(
                FabricIdentityConstants.AuthenticationSchemes.Azure,
                appConfiguration.AzureActiveDirectorySettings.DisplayName,
                options =>
                {
                    options.SignInScheme = IdentityServerConstants.ExternalCookieAuthenticationScheme;
                    options.SignOutScheme = IdentityServerConstants.SignoutScheme;
                    options.Authority = appConfiguration.AzureActiveDirectorySettings.Authority;
                    options.ResponseType = OpenIdConnectResponseType.CodeIdToken;
                    options.ClaimsIssuer = appConfiguration.AzureActiveDirectorySettings.ClaimsIssuer;
                    options.ClientId = appConfiguration.AzureActiveDirectorySettings.ClientId;
                    options.ClientSecret = appConfiguration.AzureActiveDirectorySettings.ClientSecret;
                    options.CallbackPath = "/signin-oidc-" + FabricIdentityConstants.AuthenticationSchemes.Azure;
                    options.SignedOutCallbackPath = "/signout-callback-oidc-" + FabricIdentityConstants.AuthenticationSchemes.Azure;
                    options.RemoteSignOutPath = "/signout-oidc-" + FabricIdentityConstants.AuthenticationSchemes.Azure;
                    //options.SaveTokens = true;
                    options.GetClaimsFromUserInfoEndpoint = true;
 
                    options.ClaimActions.Remove("iss");
                    //options.ClaimActions.MapUniqueJsonKey("sub", "sub");
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = false
                    };

                    foreach (var s in appConfiguration.AzureActiveDirectorySettings.Scope)
                    {
                        options.Scope.Add(s);
                    }

                });
        }

        public static AuthenticationBuilder AddExternalIdentityProviders(this AuthenticationBuilder builder,
            IAppConfiguration appConfiguration)
        {
            if (appConfiguration.ExternalIdProviderSettings?.ExternalIdProviders?.Any() == true)
            {
                // Add OpenIdConnect options to each provider.
                foreach (var externalIdProvider in appConfiguration.ExternalIdProviderSettings.ExternalIdProviders)
                {
                    builder.AddOpenIdConnect(externalIdProvider.ProviderName, externalIdProvider.DisplayName, options =>
                    {
                        options.SignInScheme = IdentityServerConstants.ExternalCookieAuthenticationScheme;
                        options.SignOutScheme = IdentityServerConstants.SignoutScheme;

                        options.Authority = externalIdProvider.Authority;
                        options.ClientId = externalIdProvider.ClientId;
                        options.ClientSecret = externalIdProvider.ClientSecret;
                        options.ResponseType = externalIdProvider.ResponseType;
                        options.CallbackPath = "/signin-oidc-" + externalIdProvider.ProviderName;
                        options.SignedOutCallbackPath = "/signout-callback-oidc-" + externalIdProvider.ProviderName;
                        options.SaveTokens = true;
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
            }

            return builder;
        }

        public static IApplicationBuilder UseStaticFilesForAcmeChallenge(this IApplicationBuilder builder, string challengeDirectory, ILogger logger)
        {
            var fullyQualifiedChallengeDirectory = Path.Combine(Directory.GetCurrentDirectory(), challengeDirectory);
            try
            {
                if (!Directory.Exists(fullyQualifiedChallengeDirectory))
                {
                    Directory.CreateDirectory(fullyQualifiedChallengeDirectory);
                }
                builder.UseStaticFiles(new StaticFileOptions
                {
                    FileProvider =
                        new PhysicalFileProvider(fullyQualifiedChallengeDirectory),
                    RequestPath = new PathString($"/{challengeDirectory}"),
                    ServeUnknownFileTypes = true
                });

            }
            catch (UnauthorizedAccessException ex)
            {
                //just log the exception, as we don't want to crash the process if we can't create this directory
                logger.Warning(ex, $"Did not have permissions to create challenge directory {fullyQualifiedChallengeDirectory}");
            }
            return builder;
        }
    }
}
