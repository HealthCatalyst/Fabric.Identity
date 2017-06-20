using System.Runtime.InteropServices;
using Fabric.Identity.API.Configuration;
using Fabric.Identity.API.Services;
using IdentityServer4.Quickstart.UI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Serilog;

namespace Fabric.Identity.API.Extensions
{
    public static class IdentityServerBuilderExtensions
    {
        public static IIdentityServerBuilder AddTestUsersIfConfigured(this IIdentityServerBuilder identityServerBuilder, HostingOptions hostingOptions)
        {
            if (hostingOptions != null && hostingOptions.UseTestUsers)
            {
                identityServerBuilder.AddTestUsers(TestUsers.Users);
            }
            return identityServerBuilder;
        }

        public static IIdentityServerBuilder AddSigningCredentialAndValidationKeys(this IIdentityServerBuilder identityServerBuilder,
            SigningCertificateSettings certificateSettings, ICertificateService certificateService, ILogger logger)
        {
            if (certificateSettings.UseTemporarySigningCredential)
            {
                logger.Information("Using temporary signing credential - this is not recommended for production");
                identityServerBuilder.AddTemporarySigningCredential();
                return identityServerBuilder;
            }

            var isLinux = RuntimeInformation.IsOSPlatform(OSPlatform.Linux);

            identityServerBuilder.AddSigningCredential(certificateService.GetCertificate(certificateSettings));
            if (HasSecondarySigningKeys(certificateSettings, isLinux))
            {
                identityServerBuilder.AddValidationKeys(
                    new X509SecurityKey(certificateService.GetCertificate(certificateSettings, isPrimary: false)));
            }

            return identityServerBuilder;
        }

        private static bool HasSecondarySigningKeys(SigningCertificateSettings certificateSettings, bool isLinux)
        {
            return isLinux && !string.IsNullOrEmpty(certificateSettings.SecondaryCertificatePath) &&
                   !string.IsNullOrEmpty(certificateSettings.SecondaryCertificatePasswordPath) || !isLinux &&
                   !string.IsNullOrEmpty(certificateSettings.SecondaryCertificateThumbprint);
        }
        
    }
}
