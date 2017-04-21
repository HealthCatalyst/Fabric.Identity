using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace Fabric.Identity.API.Configuration
{
    public class ConfigurationProvider
    {
        public IAppConfiguration GetAppConfiguration(string baseBath)
        {
            var config = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .SetBasePath(baseBath)
                .Build();

            var appConfig = new AppConfiguration();
            ConfigurationBinder.Bind(config, appConfig);
            return appConfig;
        }
    }
}
