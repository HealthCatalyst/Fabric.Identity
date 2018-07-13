using System.IO;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Fabric.Identity.API.Configuration;
using Fabric.Platform.Shared.Exceptions;
using RestSharp.Extensions;

namespace Fabric.Identity.API.Services
{
    public class LinuxCertificateService : ICertificateService
    {
        public X509Certificate2 GetSigningCertificate(SigningCertificateSettings certificateSettings, bool isPrimary)
        {
            if (isPrimary && string.IsNullOrEmpty(certificateSettings.PrimaryCertificatePath))
            {
                throw new FabricConfigurationException("You must specify a PrimaryCertificatePath when UseTemporarySigningCredential is set to false.");
            }
            if (isPrimary && string.IsNullOrEmpty(certificateSettings.PrimaryCertificatePasswordPath))
            {
                throw new FabricConfigurationException("You must specify a PrimaryCertificatePasswordPath when UseTemporarySigningCredential is set to false.");
            }

            if (!isPrimary && string.IsNullOrEmpty(certificateSettings.SecondaryCertificatePath))
            {
                throw new FabricConfigurationException("You must specify a PrimaryCertificatePath when UseTemporarySigningCredential is set to false.");
            }
            if (!isPrimary && string.IsNullOrEmpty(certificateSettings.SecondaryCertificatePasswordPath))
            {
                throw new FabricConfigurationException("You must specify a PrimaryCertificatePasswordPath when UseTemporarySigningCredential is set to false.");
            }

            return isPrimary
                ? GetCertFromFile(certificateSettings.PrimaryCertificatePath,
                    certificateSettings.PrimaryCertificatePasswordPath)
                : GetCertFromFile(certificateSettings.SecondaryCertificatePath,
                    certificateSettings.SecondaryCertificatePasswordPath);
        }

        public X509Certificate2 GetEncryptionCertificate(SigningCertificateSettings certificateSettings)
        {
            throw new FabricConfigurationException("Do not encrypt settings when running on a Linux container, instead use Docker Secrets to protect sensitive configuration settings.");
        }

        public RSA GetEncryptionCertificatePrivateKey(SigningCertificateSettings certificateSettings)
        {
            var cert = GetEncryptionCertificate(certificateSettings);
            return cert.GetRSAPrivateKey();
        }

        private X509Certificate2 GetCertFromFile(string certPath, string passwordPath)
        {
            using (var certStream = new FileStream(certPath, FileMode.Open, FileAccess.Read))
            {
                var password = File.ReadAllText(passwordPath).Trim();
                return new X509Certificate2(certStream.ReadAsBytes(), password);
            }
        }
    }
}
