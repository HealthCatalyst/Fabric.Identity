using System;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using Fabric.Identity.API.Configuration;
using Fabric.Platform.Shared.Exceptions;
using RestSharp.Extensions;
using Serilog;

namespace Fabric.Identity.API.Services
{
    public class LinuxCertificateService : ICertificateService
    {
        private readonly ILogger _logger;
        public LinuxCertificateService(ILogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }
        public X509Certificate2 GetCertificate(SigningCertificateSettings certificateSettings, bool isPrimary)
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

        private X509Certificate2 GetCertFromFile(string certPath, string passwordPath)
        {
            _logger.Information("Getting certificate from: {certPath}", certPath);
            using (var certStream = new FileStream(certPath, FileMode.Open, FileAccess.Read))
            {
                var password = File.ReadAllText(passwordPath).Trim();
                return new X509Certificate2(certStream.ReadAsBytes(), password);
            }
        }
    }
}
