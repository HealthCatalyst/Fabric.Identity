using System;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text.RegularExpressions;
using Fabric.Identity.API.Configuration;
using Fabric.Platform.Shared.Exceptions;
using IdentityModel;
using Serilog;

namespace Fabric.Identity.API.Services
{
    public class WindowsCertificateService : ICertificateService
    {

        public X509Certificate2 GetCertificate(SigningCertificateSettings certificateSettings, bool isPrimary = true)
        {
            if (isPrimary && string.IsNullOrEmpty(certificateSettings.PrimaryCertificateThumbprint))
            {
                throw new FabricConfigurationException("You must specify a PrimaryCertificateThumbprint when UseTemporarySigningCredential is set to false.");
            }
            if (!isPrimary && string.IsNullOrEmpty(certificateSettings.SecondaryCertificateThumbprint))
            {
                throw new FabricConfigurationException("You must specify a SecondardCertificateThumbprint to use AddValdationKeys.");
            }
            
            var thumbprint = GetThumbprint(certificateSettings, isPrimary);
            return X509.LocalMachine.My.Thumbprint.Find(thumbprint, validOnly: false).FirstOrDefault();
        }

        private string CleanThumbprint(string thumbprint)
        {
            
            var cleanedThumbprint = Regex.Replace(thumbprint, @"\s+", "").ToUpperInvariant();
            return cleanedThumbprint;
        }

        private string GetThumbprint(SigningCertificateSettings certificateSettings, bool isPrimary)
        {
            return isPrimary
                ? CleanThumbprint(certificateSettings.PrimaryCertificateThumbprint)
                : CleanThumbprint(certificateSettings.SecondaryCertificateThumbprint);
        }
    }
}
