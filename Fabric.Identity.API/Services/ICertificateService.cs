using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Fabric.Identity.API.Configuration;

namespace Fabric.Identity.API.Services
{
    public interface ICertificateService
    {
        X509Certificate2 GetSigningCertificate(SigningCertificateSettings certificateSettings, bool isPrimary = true);
        X509Certificate2 GetEncryptionCertificate(SigningCertificateSettings certificateSettings);
        RSA GetEncryptionCertificatePrivateKey(SigningCertificateSettings certificateSettings);
    }
}
