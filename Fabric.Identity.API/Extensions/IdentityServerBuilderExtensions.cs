using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using Fabric.Identity.API.Configuration;
using Fabric.Platform.Shared.Exceptions;
using Microsoft.Extensions.DependencyInjection;

namespace Fabric.Identity.API.Extensions
{
    public static class IdentityServerBuilderExtensions
    {
        public static IIdentityServerBuilder AddSigningCredentials(this IIdentityServerBuilder identityServerBuilder,
            SigningCertificateSettings certificateSettings)
        {
            if (certificateSettings.UseTemporarySigningCredential)
            {
                identityServerBuilder.AddTemporarySigningCredential();
                return identityServerBuilder;
            }

            var certificateStore = RuntimeInformation.IsOSPlatform(OSPlatform.Linux)
                ? StoreLocation.CurrentUser
                : StoreLocation.LocalMachine;

            if (string.IsNullOrEmpty(certificateSettings.PrimaryCertificateSubjectName))
            {
                throw new FabricConfigurationException("You must specify a PrimaryCertificate name when UseTemporarySigningCredential is set to false.");
            }

            identityServerBuilder.AddSigningCredential(certificateSettings.PrimaryCertificateSubjectName, certificateStore);
            
            return identityServerBuilder;
        }
    }
}
