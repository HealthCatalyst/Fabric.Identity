using System;
using System.Security.Cryptography;
using Fabric.Identity.API.Configuration;
using Fabric.Identity.API.Services;
using Fabric.Platform.Shared.Configuration;
using Moq;
using Newtonsoft.Json;
using Xunit;
using System.IO;
using Fabric.Platform.Shared.Configuration.Docker;
using Microsoft.Extensions.Configuration;

namespace Fabric.Identity.IntegrationTests.ServiceTests
{
    public class IdentityConfigurationProviderTests
    {
        [Fact]
        public void GetAppConfiguration_ReturnsAppConfig_Success()
        {
            // Arrange
            var privateKey = GetPrivateKey();
            var clientSecret = Guid.NewGuid().ToString();
            var appSettingsJson = GetAppSettingsJson(privateKey, clientSecret);
            var directory = Directory.CreateDirectory(Path.Combine(Directory.GetCurrentDirectory(), "test"));
            File.WriteAllText(Path.Combine(directory.FullName, "appsettings.json"), appSettingsJson);
            var mockCertificateService = GetMockCertificateService(privateKey);
            var decryptionService = new DecryptionService(mockCertificateService);
            var config = new ConfigurationBuilder()
                .AddJsonFile(Path.Combine(directory.FullName, "appsettings.json"))
                .Build();
            var identityConfigurationProvider = new IdentityConfigurationProvider(config);
            

            // Act
            var appConfig = identityConfigurationProvider.GetAppConfiguration(decryptionService);

            // Assert
            Assert.NotNull(appConfig);
            Assert.Equal(clientSecret, appConfig.IdentityServerConfidentialClientSettings.ClientSecret);
            Assert.Equal(clientSecret, appConfig.AzureActiveDirectorySettings.ClientSecret);
            Assert.Equal("InMemory", appConfig.HostingOptions.StorageProvider);
            Assert.Equal("HQCATALYST", appConfig.FilterSettings.GroupFilterSettings.Prefixes[0]);
        }

        private RSA GetPrivateKey()
        {
            var privateKey = RSA.Create();
            return privateKey;
        }

        private string EncryptString(RSA privateKey, string stringToEncrypt)
        {
            var bytesToEncrypt = System.Text.Encoding.UTF8.GetBytes(stringToEncrypt);
            return $"{DecryptionService.EncryptionPrefix}{Convert.ToBase64String(privateKey.Encrypt(bytesToEncrypt, RSAEncryptionPadding.OaepSHA1))}";
        }

        private ICertificateService GetMockCertificateService(RSA privateKey)
        {
            var mockCertificateService = new Mock<ICertificateService>();
            mockCertificateService.Setup(certificateService => certificateService.GetEncryptionCertificatePrivateKey(It.IsAny<SigningCertificateSettings>()))
                .Returns(privateKey);
            return mockCertificateService.Object;
        }

        private string GetAppSettingsJson(RSA privateKey, string clientSecret)
        {
            var appConfig = new AppConfiguration
            {
                HostingOptions = new HostingOptions
                {
                    StorageProvider = "InMemory",
                    UseIis = true
                },
                IdentityServerConfidentialClientSettings = new IdentityServerConfidentialClientSettings
                {
                    Authority = "http://locahost:5001",
                    ClientId = "test-client",
                    ClientSecret = EncryptString(privateKey, clientSecret)
                },
                AzureActiveDirectorySettings = new AzureActiveDirectorySettings
                {
                    ClientSecret = EncryptString(privateKey, clientSecret)
                },
                FilterSettings = new FilterSettings
                {
                    GroupFilterSettings = new GroupFilterSettings
                    {
                        Prefixes = new [] {"HQCATALYST"}
                    }
                }
            };
            return JsonConvert.SerializeObject(appConfig, Formatting.Indented, new JsonSerializerSettings{NullValueHandling = NullValueHandling.Ignore});
        }
    }
}
