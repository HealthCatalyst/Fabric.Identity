using Fabric.Identity.API;

namespace Fabric.Identity.IntegrationTests
{
    public class CouchDbIdentityProvidersControllerTests : IdentityProvidersControllerTests
    {
        public CouchDbIdentityProvidersControllerTests() : base(FabricIdentityConstants.StorageProviders.CouchDb)
        { }
    }
}
