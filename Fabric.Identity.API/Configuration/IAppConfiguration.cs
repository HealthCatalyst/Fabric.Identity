using Fabric.Identity.API.Persistence.CouchDb.Configuration;
using Fabric.Identity.API.Persistence.SqlServer.Configuration;
using Fabric.Platform.Shared.Configuration;

namespace Fabric.Identity.API.Configuration
{
    public interface IAppConfiguration
    {
        string IssuerUri { get; }
        bool LogToFile { get; }
        string ClientName { get; }
        string RegistrationAdminGroup { get; }
        bool AllowLocalLogin { get; }
        bool WindowsAuthenticationEnabled { get; }

        bool UseDiscoveryService { get; }

        string DiscoveryServiceEndpoint { get; }

        IdentityProviderSearchSettings IdentityProviderSearchSettings { get; }
        SigningCertificateSettings SigningCertificateSettings { get; }
        ElasticSearchSettings ElasticSearchSettings { get; }
        HostingOptions HostingOptions { get; }
        CouchDbSettings CouchDbSettings { get; }
        ExternalIdProviderSettings ExternalIdProviderSettings { get; }
        IdentityServerConfidentialClientSettings IdentityServerConfidentialClientSettings { get; }
        ApplicationInsights ApplicationInsights { get; }
        LdapSettings LdapSettings { get; }
        FilterSettings FilterSettings { get; }       
        ConnectionStrings ConnectionStrings { get; }
    }
}
