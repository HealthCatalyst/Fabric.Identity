using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using Fabric.Identity.API.Configuration;
using Fabric.Identity.API.Services;
using Fabric.Platform.Shared.Exceptions;
using IdentityModel;
using Moq;
using Serilog;
using Xunit;

namespace Fabric.Identity.UnitTests
{
    public class WindowsCertificateServiceTests
    {
        private readonly Mock<ILogger> _mockLogger = new Mock<ILogger>();

        [Theory, MemberData(nameof(SigningCredentialSettings))]
        public void GetCertificate_WithoutThumbprint_ThrowsException(SigningCertificateSettings signingCertificateSettings, bool isPrimary)
        {
            var certificateService = new WindowsCertificateService(_mockLogger.Object);
            Assert.Throws<FabricConfigurationException>(() => certificateService.GetCertificate(signingCertificateSettings, isPrimary));
        }

        public static IEnumerable<object[]> SigningCredentialSettings => new[]
        {
            new object[]
            {
                new SigningCertificateSettings
                {
                    UseTemporarySigningCredential = false
                },
                true
            },
            new object[]
            {
                new SigningCertificateSettings
                {
                    UseTemporarySigningCredential = false,
                    PrimaryCertificateThumbprint = "thumbprint"
                },
                false
            },
        };
    }
}
