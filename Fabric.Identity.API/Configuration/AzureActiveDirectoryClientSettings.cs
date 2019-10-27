using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Fabric.Identity.API.Configuration
{
    public class AzureActiveDirectoryClientSettings
    {
        public AzureActiveDirectoryClientSettings()
        {

        }

        public AzureClientApplicationSettings[] ClientAppSettings { get; set; }

        public string Authority { get; set; }

        public string TokenEndpoint { get; set; }
    }
}
