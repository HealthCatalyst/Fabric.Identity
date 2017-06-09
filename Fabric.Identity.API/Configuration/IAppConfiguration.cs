using Fabric.Platform.Shared.Configuration;

namespace Fabric.Identity.API.Configuration
{
    public interface IAppConfiguration
    {
        string IssuerUri { get; }
        bool LogToFile { get; }
        ElasticSearchSettings ElasticSearchSettings { get; }
        HostingOptions HostingOptions { get; }
        CouchDbSettings CouchDbSettings { get; }

        ExternalIdProviderSettings ExternalIdProviderSettings { get; }
    }
}
