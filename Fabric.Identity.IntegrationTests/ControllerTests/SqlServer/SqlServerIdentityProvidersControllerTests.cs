using Fabric.Identity.API;

namespace Fabric.Identity.IntegrationTests.ControllerTests.SqlServer
{
    public class SqlServerIdentityProvidersControllerTests : InMemory.IdentityProvidersControllerTests
    {
        public SqlServerIdentityProvidersControllerTests(string provider = FabricIdentityConstants.StorageProviders.SqlServer)
            : base(provider)
        {
        }
    }
}
