using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Fabric.Identity.API.Services;
using Fabric.Platform.Shared.Configuration.Docker;
using Microsoft.Extensions.Configuration;

namespace Fabric.Identity.API.Configuration
{
    public class IdentityConfigurationProvider
    {
        private static readonly string EncryptionPrefix = "!!enc!!:";
        
        public IAppConfiguration GetAppConfiguration(string baseBath, ICertificateService certificateService)
        {
            var appConfig = BuildAppConfiguration(baseBath);
            DecryptEncryptedValues(appConfig, certificateService);
            return appConfig;
        }

        public IAppConfiguration GetAppConfiguration(string basePath)
        {
            return BuildAppConfiguration(basePath);
        }

        private IAppConfiguration BuildAppConfiguration(string baseBath)
        {
                var config = new ConfigurationBuilder()
                    .AddJsonFile("appsettings.json")
                    .AddEnvironmentVariables()
                    .AddDockerSecrets(typeof(IAppConfiguration))
                    .SetBasePath(baseBath)
                    .Build();

                var appConfig = new AppConfiguration();
                ConfigurationBinder.Bind(config, appConfig);
                return appConfig;
        }

        private void DecryptEncryptedValues(IAppConfiguration appConfiguration, ICertificateService certificateService)
        {
            if (appConfiguration.ElasticSearchSettings != null &&
                IsEncrypted(appConfiguration.ElasticSearchSettings.Password))
            {
                appConfiguration.ElasticSearchSettings.Password =
                    DecryptString(appConfiguration.ElasticSearchSettings.Password, certificateService, appConfiguration);
            }

            if (appConfiguration.CouchDbSettings != null && IsEncrypted(appConfiguration.CouchDbSettings.Password))
            {
                appConfiguration.CouchDbSettings.Password = DecryptString(appConfiguration.CouchDbSettings.Password, certificateService, appConfiguration);
            }

            if (appConfiguration.LdapSettings != null && IsEncrypted(appConfiguration.LdapSettings.Password))
            {
                appConfiguration.LdapSettings.Password = DecryptString(appConfiguration.LdapSettings.Password,
                    certificateService, appConfiguration);
            }
        }

        private static bool IsEncrypted(string value)
        {
            return !string.IsNullOrEmpty(value) && value.StartsWith(EncryptionPrefix);
        }

        private string DecryptString(string encryptedString, ICertificateService certificateService, IAppConfiguration appConfiguration)
        {

            var cert = certificateService.GetEncryptionCertificate(appConfiguration.SigningCertificateSettings);
            var encryptedPasswordAsBytes =
                System.Convert.FromBase64String(
                    encryptedString.TrimStart(EncryptionPrefix.ToCharArray()));
            var decryptedPasswordAsBytes = cert.GetRSAPrivateKey().Decrypt(encryptedPasswordAsBytes, RSAEncryptionPadding.OaepSHA1);
            return System.Text.Encoding.UTF8.GetString(decryptedPasswordAsBytes);
        }
    }
}
