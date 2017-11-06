using Fabric.Identity.API;
using Fabric.Identity.IntegrationTests.ControllerTests.InMemory;

namespace Fabric.Identity.IntegrationTests.ControllerTests.CouchDb
{
    public class CouchDbApiResourceControllerTests : ApiResourceControllerTests
    {
        public CouchDbApiResourceControllerTests() : base(FabricIdentityConstants.StorageProviders.CouchDb)
        { }
    }
}
