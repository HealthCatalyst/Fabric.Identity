using Fabric.Identity.API;

namespace Fabric.Identity.IntegrationTests
{
    public class CouchDbIdentityResourceControllerTests : IdentityResourceControllerTests
    {
        public CouchDbIdentityResourceControllerTests() : base(FabricIdentityConstants.StorageProviders.CouchDb)
        { }
    }
}
