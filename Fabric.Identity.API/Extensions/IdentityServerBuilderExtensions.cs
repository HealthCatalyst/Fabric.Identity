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

namespace Fabric.Identity.API.Extensions
{
    public static class IdentityServerBuilderExtensions
    {
        public static IIdentityServerBuilder AddSigningCredentialAndValidationKeys(this IIdentityServerBuilder identityServerBuilder,
            SigningCertificateSettings certificateSettings)
        {
            if (certificateSettings.UseTemporarySigningCredential)
            {
                identityServerBuilder.AddTemporarySigningCredential();
                return identityServerBuilder;
            }

            return RuntimeInformation.IsOSPlatform(OSPlatform.Linux)
                ? identityServerBuilder.AddSigningCredentialLinux(certificateSettings)
                    .AddValidationKeysLinux(certificateSettings)
                : identityServerBuilder.AddSigningCredentialWindows(certificateSettings)
                    .AddValidationKeysWindows(certificateSettings);
        }

        public static IIdentityServerBuilder AddSigningCredentialWindows(
            this IIdentityServerBuilder identityServerBuilder, SigningCertificateSettings certificateSettings)
        {
            if (string.IsNullOrEmpty(certificateSettings.PrimaryCertificateThumbprint))
            {
                throw new FabricConfigurationException("You must specify a PrimaryCertificate name when UseTemporarySigningCredential is set to false.");
            }
            var cleanedThumbprint = CleanThumbprint(certificateSettings.PrimaryCertificateThumbprint);
            identityServerBuilder.AddSigningCredential(cleanedThumbprint, StoreLocation.LocalMachine, NameType.Thumbprint);

            return identityServerBuilder;
        }

        public static IIdentityServerBuilder AddSigningCredentialLinux(
            this IIdentityServerBuilder identityServerBuilder, SigningCertificateSettings certificateSettings)
        {
            if (string.IsNullOrEmpty(certificateSettings.PrimaryCertificatePath))
            {
                throw new FabricConfigurationException("You must specify a PrimaryCertificatePath when UseTemporarySigningCredential is set to false.");
            }
            if (string.IsNullOrEmpty(certificateSettings.PrimaryCertificatePasswordPath))
            {
                throw new FabricConfigurationException("You must specify a PrimaryCertificatePasswordPath when UseTemporarySigningCredential is set to false.");
            }

            var signingCert = GetCertFromFile(certificateSettings.PrimaryCertificatePath,
                certificateSettings.PrimaryCertificatePasswordPath);
            return identityServerBuilder.AddSigningCredential(signingCert);
        }

        public static IIdentityServerBuilder AddValidationKeysWindows(this IIdentityServerBuilder identityServerBuilder,
            SigningCertificateSettings certificateSettings)
        {
            if (!string.IsNullOrEmpty(certificateSettings.SecondaryCertificateThumbprint))
            {
                var cleanedThumbprint = CleanThumbprint(certificateSettings.SecondaryCertificateThumbprint);
                var signingCert = X509.LocalMachine.My.Thumbprint
                    .Find(cleanedThumbprint, validOnly: false)
                    .FirstOrDefault();
                identityServerBuilder.AddValidationKeys(new X509SecurityKey(signingCert));
            }
            return identityServerBuilder;
        }

        public static IIdentityServerBuilder AddValidationKeysLinux(this IIdentityServerBuilder identityServerBuilder,
            SigningCertificateSettings certificateSettings)
        {
            if (!string.IsNullOrEmpty(certificateSettings.SecondaryCertificatePath) &&
                !string.IsNullOrEmpty(certificateSettings.SecondaryCertificatePasswordPath))
            {
                var signingCert = GetCertFromFile(certificateSettings.SecondaryCertificatePath,
                    certificateSettings.SecondaryCertificatePasswordPath);
                identityServerBuilder.AddValidationKeys(new X509SecurityKey(signingCert));
            }
            return identityServerBuilder;
        }

        private static X509Certificate2 GetCertFromFile(string certPath, string passwordPath)
        {
            var certStream = new FileStream(certPath, FileMode.Open);
            var password = File.ReadAllText(passwordPath);

            return new X509Certificate2(certStream.ReadAsBytes(), password);
        }

        private static string CleanThumbprint(string thumbprint)
        {
            return Regex.Replace(thumbprint, @"\s+", "").ToUpperInvariant();
        }
    }
}
