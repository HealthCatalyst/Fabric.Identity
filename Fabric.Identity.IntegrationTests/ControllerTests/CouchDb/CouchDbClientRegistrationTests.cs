using Fabric.Identity.API;
using Fabric.Identity.IntegrationTests.ControllerTests.InMemory;

namespace Fabric.Identity.IntegrationTests.ControllerTests.CouchDb
{
    public class CouchDbClientRegistrationTests : ClientRegistrationTests
    {
        public CouchDbClientRegistrationTests() : base(FabricIdentityConstants.StorageProviders.CouchDb)
        { }
    }
}
