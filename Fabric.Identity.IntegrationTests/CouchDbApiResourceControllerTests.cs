using Fabric.Identity.API;

namespace Fabric.Identity.IntegrationTests
{
    public class CouchDbApiResourceControllerTests : ApiResourceControllerTests
    {
        public CouchDbApiResourceControllerTests() : base(FabricIdentityConstants.StorageProviders.CouchDb)
        { }
    }
}
