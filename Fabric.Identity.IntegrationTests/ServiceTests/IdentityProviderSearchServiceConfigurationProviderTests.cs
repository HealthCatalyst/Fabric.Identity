using Fabric.IdentityProviderSearchService.Configuration;
using Fabric.IdentityProviderSearchService.Services;
using Fabric.Platform.Shared.Configuration;
using Microsoft.Extensions.Configuration;
using Moq;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using Xunit;

namespace Fabric.IdentityProviderSearchService.IntegrationTests.ServiceTests
{
    public class IdentityProviderSearchServiceConfigurationProviderTests
    {
        [Theory]
        [InlineData(1)]
        [InlineData(2)]
        public void ConfigurationProvider_DecryptsSecret_Successfully(int count)
        {
            var privateKey = GetPrivateKey();
            var clientSecret = Guid.NewGuid().ToString();
            var appSettingsJson = GetEncryptedAppSettings(privateKey, clientSecret, count);
            var directory = Directory.CreateDirectory(Path.Combine(Directory.GetCurrentDirectory(), "test"));
            File.WriteAllText(Path.Combine(directory.FullName, "appsettings.json"), appSettingsJson);
            var mockCertificateService = GetMockCertificateService(privateKey);
            var decryptionService = new DecryptionService(mockCertificateService);

            var appConfig = GetAppConfiguration();
            var configProvider = new IdentityProviderSearchServiceConfigurationProvider(new EncryptionCertificateSettings(), decryptionService);

            // Act
            configProvider.GetAppConfiguration(appConfig);

            // Assert
            Assert.NotNull(appConfig);
            for (int i = 0; i < count; i++)
            {
                Assert.Equal(clientSecret, appConfig.AzureActiveDirectoryClientSettings.ClientAppSettings[i].ClientSecret);
            }
        }

        private ICertificateService GetMockCertificateService(RSA privateKey)
        {
            var mockCertificateService = new Mock<ICertificateService>();
            mockCertificateService.Setup(certificateService => certificateService.GetEncryptionCertificatePrivateKey(It.IsAny<EncryptionCertificateSettings>()))
                .Returns(privateKey);
            return mockCertificateService.Object;
        }

        private string GetEncryptedAppSettings(RSA privateKey, string clientSecret, int appSettingsCount = 1)
        {
            var appSettingsList = new List<AzureClientApplicationSettings>();
            for(int i = 0; i < appSettingsCount; i++)
            {
                appSettingsList.Add(new AzureClientApplicationSettings
                {
                    ClientId = "test-client" + i,
                    TenantId = "tenantid" + i,
                    ClientSecret = EncryptString(privateKey, clientSecret)
                });
            }

            var appConfig = new AppConfiguration
            {
                AzureActiveDirectoryClientSettings = new AzureActiveDirectoryClientSettings
                {
                    ClientAppSettings = appSettingsList.ToArray()
                }
            };
            return JsonConvert.SerializeObject(appConfig, Formatting.Indented, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });
        }

        private string EncryptString(RSA privateKey, string stringToEncrypt)
        {
            var bytesToEncrypt = System.Text.Encoding.UTF8.GetBytes(stringToEncrypt);
            return $"{DecryptionService.EncryptionPrefix}{Convert.ToBase64String(privateKey.Encrypt(bytesToEncrypt, RSAEncryptionPadding.OaepSHA1))}";
        }

        private AppConfiguration GetAppConfiguration()
        {
            var configuration = new ConfigurationBuilder()
                .AddJsonFile(Directory.CreateDirectory(Path.Combine(Directory.GetCurrentDirectory(), "test")).FullName + "/appsettings.json")
                .Add(new WebConfigProvider())
                .Build();

            var appConfig = new AppConfiguration();
            ConfigurationBinder.Bind(configuration, appConfig);

            return appConfig;
        }

        private RSA GetPrivateKey()
        {
            var privateKey = RSA.Create();
            return privateKey;
        }
    }
}
