using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using System.Text.RegularExpressions;
using Fabric.Identity.API.Configuration;
using Fabric.Platform.Shared.Exceptions;
using IdentityModel;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using RestSharp.Extensions;
using Serilog;

namespace Fabric.Identity.API.Extensions
{
    public static class IdentityServerBuilderExtensions
    {
        public static IIdentityServerBuilder AddSigningCredentialAndValidationKeys(this IIdentityServerBuilder identityServerBuilder,
            SigningCertificateSettings certificateSettings, ILogger logger)
        {
            if (certificateSettings.UseTemporarySigningCredential)
            {
                logger.Information("Using temporary signing credential - this is not recommended for production");
                identityServerBuilder.AddTemporarySigningCredential();
                return identityServerBuilder;
            }

            return RuntimeInformation.IsOSPlatform(OSPlatform.Linux)
                ? identityServerBuilder.AddSigningCredentialLinux(certificateSettings, logger)
                    .AddValidationKeysLinux(certificateSettings, logger)
                : identityServerBuilder.AddSigningCredentialWindows(certificateSettings, logger)
                    .AddValidationKeysWindows(certificateSettings, logger);
        }

        public static IIdentityServerBuilder AddSigningCredentialWindows(
            this IIdentityServerBuilder identityServerBuilder, SigningCertificateSettings certificateSettings, ILogger logger)
        {
            logger.Information("Configuring signing credentials for Windows platform");
            if (string.IsNullOrEmpty(certificateSettings.PrimaryCertificateThumbprint))
            {
                throw new FabricConfigurationException("You must specify a PrimaryCertificate name when UseTemporarySigningCredential is set to false.");
            }
            var cleanedThumbprint = CleanThumbprint(certificateSettings.PrimaryCertificateThumbprint);
            identityServerBuilder.AddSigningCredential(cleanedThumbprint, StoreLocation.LocalMachine, NameType.Thumbprint);

            return identityServerBuilder;
        }

        public static IIdentityServerBuilder AddSigningCredentialLinux(
            this IIdentityServerBuilder identityServerBuilder, SigningCertificateSettings certificateSettings, ILogger logger)
        {
            logger.Information("Configuring signing credentials for Linux platform");
            if (string.IsNullOrEmpty(certificateSettings.PrimaryCertificatePath))
            {
                throw new FabricConfigurationException("You must specify a PrimaryCertificatePath when UseTemporarySigningCredential is set to false.");
            }
            if (string.IsNullOrEmpty(certificateSettings.PrimaryCertificatePasswordPath))
            {
                throw new FabricConfigurationException("You must specify a PrimaryCertificatePasswordPath when UseTemporarySigningCredential is set to false.");
            }

            var signingCert = GetCertFromFile(certificateSettings.PrimaryCertificatePath,
                certificateSettings.PrimaryCertificatePasswordPath, logger);
            return identityServerBuilder.AddSigningCredential(signingCert);
        }

        public static IIdentityServerBuilder AddValidationKeysWindows(this IIdentityServerBuilder identityServerBuilder,
            SigningCertificateSettings certificateSettings, ILogger logger)
        {
            if (!string.IsNullOrEmpty(certificateSettings.SecondaryCertificateThumbprint))
            {
                logger.Information("Adding additional validation keys for Windows platform");
                var cleanedThumbprint = CleanThumbprint(certificateSettings.SecondaryCertificateThumbprint);
                var signingCert = X509.LocalMachine.My.Thumbprint
                    .Find(cleanedThumbprint, validOnly: false)
                    .FirstOrDefault();
                identityServerBuilder.AddValidationKeys(new X509SecurityKey(signingCert));
            }
            return identityServerBuilder;
        }

        public static IIdentityServerBuilder AddValidationKeysLinux(this IIdentityServerBuilder identityServerBuilder,
            SigningCertificateSettings certificateSettings, ILogger logger)
        {
            if (!string.IsNullOrEmpty(certificateSettings.SecondaryCertificatePath) &&
                !string.IsNullOrEmpty(certificateSettings.SecondaryCertificatePasswordPath))
            {
                logger.Information("Adding additional validation keys for Linux platform");
                var signingCert = GetCertFromFile(certificateSettings.SecondaryCertificatePath,
                    certificateSettings.SecondaryCertificatePasswordPath, logger);
                identityServerBuilder.AddValidationKeys(new X509SecurityKey(signingCert));
            }
            return identityServerBuilder;
        }

        private static X509Certificate2 GetCertFromFile(string certPath, string passwordPath, ILogger logger)
        {
            var certStream = new FileStream(certPath, FileMode.Open, FileAccess.Read);
            var password = File.ReadAllText(passwordPath).Trim();
            logger.Information("Password for pfx is: {password}", password);
            return new X509Certificate2(certStream.ReadAsBytes(), password);
        }

        private static string CleanThumbprint(string thumbprint)
        {
            return Regex.Replace(thumbprint, @"\s+", "").ToUpperInvariant();
        }
    }
}
