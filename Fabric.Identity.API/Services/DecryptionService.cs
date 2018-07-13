using System;
using System.Security.Cryptography;
using Fabric.Identity.API.Configuration;

namespace Fabric.Identity.API.Services
{
    public class DecryptionService
    {
        public static readonly string EncryptionPrefix = "!!enc!!:";
        private readonly ICertificateService _certificateService;
        public DecryptionService(ICertificateService certificateService)
        {
            _certificateService = certificateService ?? throw new ArgumentNullException(nameof(certificateService));
        }

        private bool IsEncrypted(string value)
        {
            return !string.IsNullOrEmpty(value) && value.StartsWith(EncryptionPrefix);
        }

        public string DecryptString(string encryptedString, SigningCertificateSettings signingCertificateSettings)
        {
            if (!IsEncrypted(encryptedString)) return encryptedString;

            var privateKey =
                _certificateService.GetEncryptionCertificatePrivateKey(signingCertificateSettings);
            var encryptedPasswordAsBytes =
                Convert.FromBase64String(
                    encryptedString.Replace(EncryptionPrefix, string.Empty));
            var decryptedPasswordAsBytes =
                privateKey.Decrypt(encryptedPasswordAsBytes, RSAEncryptionPadding.OaepSHA1);
            return System.Text.Encoding.UTF8.GetString(decryptedPasswordAsBytes);
        }
    }
}
