using Fabric.Identity.API;

namespace Fabric.Identity.IntegrationTests.ControllerTests.SqlServer
{
    public class SqlServerIdentityResourceControllerTests : InMemory.IdentityResourceControllerTests
    {
        public SqlServerIdentityResourceControllerTests(string provider = FabricIdentityConstants.StorageProviders.SqlServer) 
            : base(provider)
        {
        }
    }
}
