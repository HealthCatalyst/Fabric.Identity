using Microsoft.Extensions.Configuration;

namespace Fabric.Identity.API.Configuration
{
    public class ConfigurationProvider
    {
        public IAppConfiguration GetAppConfiguration(string baseBath)
        {
            var config = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .AddEnvironmentVariables()
                .SetBasePath(baseBath)
                .Build();

            var appConfig = new AppConfiguration();
            ConfigurationBinder.Bind(config, appConfig);
            return appConfig;
        }
    }
}
