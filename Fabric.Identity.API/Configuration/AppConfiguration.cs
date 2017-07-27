using Fabric.Platform.Shared.Configuration;

namespace Fabric.Identity.API.Configuration
{
    public class AppConfiguration : IAppConfiguration
    {
        public string IssuerUri { get; set; }
        public bool LogToFile { get; set; }
        public string ClientName { get; set; }
        public string RegistrationAdminGroup { get; set; }
        public bool AllowLocalLogin { get; set; }
        public SigningCertificateSettings SigningCertificateSettings { get; set; }
        public ElasticSearchSettings ElasticSearchSettings { get; set; }
        public HostingOptions HostingOptions { get; set; }
        public CouchDbSettings CouchDbSettings { get; set; }
        public ExternalIdProviderSettings ExternalIdProviderSettings { get; set; }
        public IdentityServerConfidentialClientSettings IdentityServerConfidentialClientSettings { get; set; }
        public ApplicationInsights ApplicationInsights { get; set; }
    }
}
