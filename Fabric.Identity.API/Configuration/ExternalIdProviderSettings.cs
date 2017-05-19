using System.Collections.Generic;

namespace Fabric.Identity.API.Configuration
{
    public class ExternalIdProviderSettings
    {
        public IEnumerable<ExternalIdProviderSetting> ExternalIdProviders { get; set; }
    }

    public class ExternalIdProviderSetting
    {
        public string Type { get; set; }
        public string DisplayName { get; set; }
        public string Authority { get; set; }
        public string ClientId { get; set; }
        public string ClientSecret { get; set; }
        public string ResponseType { get; set; }
        public string[] Scope { get; set; }
    }
}
