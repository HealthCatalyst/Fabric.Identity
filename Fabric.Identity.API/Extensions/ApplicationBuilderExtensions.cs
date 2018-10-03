using System;
using System.IO;
using System.Linq;
using Fabric.Identity.API.Configuration;
using IdentityServer4;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

using Microsoft.Extensions.FileProviders;
using Microsoft.IdentityModel.Tokens;
using Serilog;

namespace Fabric.Identity.API.Extensions
{
    using Microsoft.IdentityModel.Protocols.OpenIdConnect;

    public static class ApplicationBuilderExtensions
    {


        public static IApplicationBuilder UseAzureIdentityProvider(this IApplicationBuilder builder,
                                                                       IAppConfiguration appConfiguration)
        {


            var options = new OpenIdConnectOptions
            {
                SignInScheme = IdentityServerConstants.ExternalCookieAuthenticationScheme,
                SignOutScheme = IdentityServerConstants.SignoutScheme,

                DisplayName = "rorbakeroutlook",
                Authority = "https://login.microsoftonline.com/common",
                ResponseType = OpenIdConnectResponseType.CodeIdToken,
                ClaimsIssuer = "the issuer",
                ClientId = "some client",
                ClientSecret = "dont tell anyone",
                GetClaimsFromUserInfoEndpoint = true,
                TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = false
                }
            };
            options.Scope.Add("openid");
            options.Scope.Add("profile");
            
            builder.UseOpenIdConnectAuthentication(options);

            return builder;
        }

        public static IApplicationBuilder UseExternalIdentityProviders(this IApplicationBuilder builder,
            IAppConfiguration appConfiguration)
        {
            if (appConfiguration.ExternalIdProviderSettings?.ExternalIdProviders?.Any() == true)
            {
                // Add OpenIdConnect options to each provider.
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
