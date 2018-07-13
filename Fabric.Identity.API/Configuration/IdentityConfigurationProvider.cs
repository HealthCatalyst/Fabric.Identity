﻿using Fabric.Identity.API.Services;
using Fabric.Platform.Shared.Configuration.Docker;
using Microsoft.Extensions.Configuration;

namespace Fabric.Identity.API.Configuration
{
    public class IdentityConfigurationProvider
    {
        public IAppConfiguration GetAppConfiguration(string baseBath, DecryptionService decryptionService)
        {
            var appConfig = BuildAppConfiguration(baseBath);
            DecryptEncryptedValues(appConfig, decryptionService);
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
        }
    }
}
