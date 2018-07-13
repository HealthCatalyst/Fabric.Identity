using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using EnsureThat;
using Fabric.Identity.API.Configuration;
using Fabric.Identity.API.Services;
using Xunit;
using Moq;

namespace Fabric.Identity.UnitTests.Services
{
    public class DecryptionServiceTests
    {
        [Fact]
        public void DecryptionService_ThrowsArgumentNullException_NullCertificateService()
        {
            Assert.Throws<ArgumentNullException>(() => new DecryptionService(null));
        }
        [Fact]
        public void DecryptString_DecryptsEncryptedString()
        {
            // Arrange
            var privateKey = GetPrivateKey();
            var stringToEncrypt = Guid.NewGuid().ToString();
            var encryptedString = $"{DecryptionService.EncryptionPrefix}{EncryptString(privateKey, stringToEncrypt)}";

            var mockCertificateService = GetMockCertificateService(privateKey);
            var signingCertificateSettings = new SigningCertificateSettings();
            var decryptionService = new DecryptionService(mockCertificateService);

            // Act
            var decryptedString = decryptionService.DecryptString(encryptedString, signingCertificateSettings);

            // Assert
            Assert.Equal(stringToEncrypt, decryptedString);
        }

        [Fact]
        public void DecryptString_ReturnsNonEncryptedString()
        {
            // Arrange
            var privateKey = GetPrivateKey();
            var clearTextstringToDecrypt = Guid.NewGuid().ToString();

            var mockCertificateService = GetMockCertificateService(privateKey);
            var signingCertificateSettings = new SigningCertificateSettings();
            var decryptionService = new DecryptionService(mockCertificateService);

            // Act
            var decryptedString = decryptionService.DecryptString(clearTextstringToDecrypt, signingCertificateSettings);

            // Assert
            Assert.Equal(clearTextstringToDecrypt, decryptedString);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData(" ")]
        public void DecryptString_HandlesNullOrEmptyString(string stringToDecrypt)
        {
            // Arrange
            var privateKey = GetPrivateKey();
            var mockCertificateService = GetMockCertificateService(privateKey);
            var signingCertificateSettings = new SigningCertificateSettings();
            var decryptionService = new DecryptionService(mockCertificateService);

            // Act
            var decryptedString = decryptionService.DecryptString(stringToDecrypt, signingCertificateSettings);

            // Assert
            Assert.Equal(stringToDecrypt, decryptedString);
        }

        [Fact]
        public void DecryptString_ThrowsFormatException_InvalidBase64String()
        {
            // Arrange
            var privateKey = GetPrivateKey();
            var stringToEncrypt = Guid.NewGuid().ToString();
            var encryptedString = $"{DecryptionService.EncryptionPrefix}{EncryptString(privateKey, stringToEncrypt).Substring(1)}";

            var mockCertificateService = GetMockCertificateService(privateKey);
            var signingCertificateSettings = new SigningCertificateSettings();
            var decryptionService = new DecryptionService(mockCertificateService);

            // Act & Assert
            Assert.Throws<FormatException>(
                () => decryptionService.DecryptString(encryptedString, signingCertificateSettings));

        }

        private RSA GetPrivateKey()
        {
            var privateKey = RSA.Create();
            return privateKey;
        }

        private string EncryptString(RSA privateKey, string stringToEncrypt)
        {
            var bytesToEncrypt = System.Text.Encoding.UTF8.GetBytes(stringToEncrypt);
            return Convert.ToBase64String(privateKey.Encrypt(bytesToEncrypt, RSAEncryptionPadding.OaepSHA1));
        }

        private ICertificateService GetMockCertificateService(RSA privateKey)
        {
            var mockCertificateService = new Mock<ICertificateService>();
            mockCertificateService.Setup(certificateService => certificateService.GetEncryptionCertificatePrivateKey(It.IsAny<SigningCertificateSettings>()))
                .Returns(privateKey);
            return mockCertificateService.Object;
        }
    }
}
