using System.Collections.Generic;
using Fabric.Identity.API.Configuration;
using Fabric.Identity.API.Services;
using Fabric.Platform.Shared.Exceptions;
using Xunit;

namespace Fabric.Identity.UnitTests
{
    public class WindowsCertificateServiceTests
    {

        [Theory, MemberData(nameof(SigningCredentialSettings))]
        public void GetCertificate_WithoutThumbprint_ThrowsException(SigningCertificateSettings signingCertificateSettings, bool isPrimary)
        {
            var certificateService = new WindowsCertificateService();
            Assert.Throws<FabricConfigurationException>(() => certificateService.GetSigningCertificate(signingCertificateSettings, isPrimary));
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
