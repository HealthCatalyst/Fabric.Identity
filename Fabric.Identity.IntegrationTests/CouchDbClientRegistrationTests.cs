using Fabric.Identity.API;

namespace Fabric.Identity.IntegrationTests
{
    public class CouchDbClientRegistrationTests : ClientRegistrationTests
    {
        public CouchDbClientRegistrationTests() : base(FabricIdentityConstants.StorageProviders.CouchDb)
        { }
    }
}
