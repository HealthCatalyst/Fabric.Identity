using Fabric.Identity.API.Services;
using Fabric.Platform.Shared.Configuration.Docker;
using Microsoft.Extensions.Configuration;

namespace Fabric.Identity.API.Configuration
{

    public class IdentityConfigurationProvider
    {
        private IConfiguration _configuration;

        public IdentityConfigurationProvider(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public IAppConfiguration GetAppConfiguration(DecryptionService decryptionService)
        {
            var appConfig = BuildAppConfiguration();
            DecryptEncryptedValues(appConfig, decryptionService);
            return appConfig;
        }

        public IAppConfiguration GetAppConfiguration()
        {
            return BuildAppConfiguration();
        }

        private IAppConfiguration BuildAppConfiguration()
        {
            var appConfig = new AppConfiguration();
            _configuration.Bind(appConfig);
            return appConfig;
        }

        private void DecryptEncryptedValues(IAppConfiguration appConfiguration, DecryptionService decryptionService)
        {
            if (appConfiguration.ElasticSearchSettings != null)
            {
                appConfiguration.ElasticSearchSettings.Password = decryptionService.DecryptString(
                    appConfiguration.ElasticSearchSettings.Password, appConfiguration.SigningCertificateSettings);
            }

            if (appConfiguration.CouchDbSettings != null)
            {
                appConfiguration.CouchDbSettings.Password = decryptionService.DecryptString(
                    appConfiguration.CouchDbSettings.Password, appConfiguration.SigningCertificateSettings);
            }

            if (appConfiguration.LdapSettings != null)
            {
                appConfiguration.LdapSettings.Password = decryptionService.DecryptString(
                    appConfiguration.LdapSettings.Password, appConfiguration.SigningCertificateSettings);
            }

            if (appConfiguration.IdentityServerConfidentialClientSettings != null)
            {
                appConfiguration.IdentityServerConfidentialClientSettings.ClientSecret =
                    decryptionService.DecryptString(
                        appConfiguration.IdentityServerConfidentialClientSettings.ClientSecret,
                        appConfiguration.SigningCertificateSettings);
            }

            if (appConfiguration.AzureActiveDirectorySettings?.ClientSecret != null)
            {
                appConfiguration.AzureActiveDirectorySettings.ClientSecret = decryptionService.DecryptString(
                    appConfiguration.AzureActiveDirectorySettings.ClientSecret,
                    appConfiguration.SigningCertificateSettings);
            }
        }
    }
}
