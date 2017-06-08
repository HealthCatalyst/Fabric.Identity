using Fabric.Platform.Shared.Configuration;

namespace Fabric.Identity.API.Configuration
{
    public class AppConfiguration : IAppConfiguration
    {
        public string IssuerUri { get; set; }
        public ElasticSearchSettings ElasticSearchSettings { get; set; }
        public HostingOptions HostingOptions { get; set; }
        public CouchDbSettings CouchDbSettings { get; set; }
        public ExternalIdProviderSettings ExternalIdProviderSettings { get; set; }
    }
}
