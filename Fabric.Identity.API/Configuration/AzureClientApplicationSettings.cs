using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Fabric.Identity.API.Configuration
{
    public class AzureClientApplicationSettings
    {
        public AzureClientApplicationSettings()
        {

        }

        public string ClientId { get; set; }
        public string ClientSecret { get; set; }
        public string TenantId { get; set; }
        public string TenantAlias { get; set; }
        public string[] Scopes { get; set; }

        public static IDictionary<string, AzureClientApplicationSettings> CreateDictionary(AzureActiveDirectoryClientSettings azureClientSettings)
        {
            var result = new Dictionary<string, AzureClientApplicationSettings>();
            foreach (var app in azureClientSettings.ClientAppSettings)
            {
                result.Add(app.TenantId, app);
            }
            return result;
        }

    }
}
