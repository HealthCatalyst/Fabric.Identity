using Fabric.Identity.API;

namespace Fabric.Identity.IntegrationTests.ControllerTests.SqlServer
{
    public class SqlServerClientRegistrationTests : InMemory.ClientRegistrationTests
    {
        public SqlServerClientRegistrationTests(string storageProvider = FabricIdentityConstants.StorageProviders.SqlServer)
            : base(storageProvider)
        {
        }
    }
}
