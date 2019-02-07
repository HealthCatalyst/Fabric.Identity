using System;
using System.IO;
using System.Linq;
using Fabric.Identity.API.Configuration;
using IdentityServer4;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
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
