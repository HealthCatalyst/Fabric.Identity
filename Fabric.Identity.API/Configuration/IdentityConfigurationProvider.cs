using Fabric.Platform.Shared.Configuration.Docker;
using Microsoft.Extensions.Configuration;

namespace Fabric.Identity.API.Configuration
{
    public class IdentityConfigurationProvider
    {
        public IAppConfiguration GetAppConfiguration(string baseBath)
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
    }
}
