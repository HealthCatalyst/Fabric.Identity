using Fabric.Platform.Shared.Configuration;

namespace Fabric.Identity.API.Configuration
{
    public interface IAppConfiguration
    {
        ElasticSearchSettings ElasticSearchSettings { get; }
        HostingOptions HostingOptions { get; }
        CouchDbSettings CouchDbSettings { get; }
    }
}
