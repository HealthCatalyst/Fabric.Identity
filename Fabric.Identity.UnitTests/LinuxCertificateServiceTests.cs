using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Fabric.Identity.API.Configuration;
using Fabric.Identity.API.Services;
using Fabric.Platform.Shared.Exceptions;
using Moq;
using Serilog;
using Xunit;

namespace Fabric.Identity.UnitTests
{
    public class LinuxCertificateServiceTests
    {

        [Theory, MemberData(nameof(SigningCredentialSettingsConfigurationException))]
        public void GetCertificate_WithoutCertPath_ThrowsFabricConfigurationException(SigningCertificateSettings signingCertificateSettings, bool isPrimary)
        {
            var certificateService = new LinuxCertificateService();
            Assert.Throws<FabricConfigurationException>(() => certificateService.GetCertificate(signingCertificateSettings, isPrimary));
        }

        [Theory, MemberData(nameof(SigningCredentialSettingsNotFoundException))]
        public void GetCertificate_WithoutCertPath_ThrowsFileNotFoundException(SigningCertificateSettings signingCertificateSettings, bool isPrimary)
        {
            var certificateService = new LinuxCertificateService();
            Assert.Throws<FileNotFoundException>(() => certificateService.GetCertificate(signingCertificateSettings, isPrimary));
        }

        public static IEnumerable<object[]> SigningCredentialSettingsConfigurationException => new[]
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
                    PrimaryCertificatePath = "somepath"
                },
                true
            },new object[]
            {
                new SigningCertificateSettings
                {
                    UseTemporarySigningCredential = false,
                    PrimaryCertificatePath = "somepath",
                    PrimaryCertificatePasswordPath = "somepath"
                },
                false
            },new object[]
            {
                new SigningCertificateSettings
                {
                    UseTemporarySigningCredential = false,
                    PrimaryCertificatePath = "somepath",
                    PrimaryCertificatePasswordPath = "somepath",
                    SecondaryCertificatePath = "somepath"
                },
                false
            }
        };

        public static IEnumerable<object[]> SigningCredentialSettingsNotFoundException => new[]
        {
            new object[]
            {
                new SigningCertificateSettings
                {
                    UseTemporarySigningCredential = false,
                    PrimaryCertificatePath = "somepath",
                    PrimaryCertificatePasswordPath = "somepath",
                    SecondaryCertificatePath = "somepath",
                    SecondaryCertificatePasswordPath = "somepath"
                },
                false
            },new object[]
            {
                new SigningCertificateSettings
                {
                    UseTemporarySigningCredential = false,
                    PrimaryCertificatePath = "somepath",
                    PrimaryCertificatePasswordPath = "somepath"
                },
                true
            }
        };
    }
}
