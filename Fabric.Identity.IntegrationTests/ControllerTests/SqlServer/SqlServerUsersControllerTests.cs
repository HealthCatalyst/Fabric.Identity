using Fabric.Identity.API;

namespace Fabric.Identity.IntegrationTests.ControllerTests.SqlServer
{
    public class SqlServerUsersControllerTests : InMemory.UsersControllerTests
    {
        public SqlServerUsersControllerTests(string provider = FabricIdentityConstants.StorageProviders.SqlServer) 
            : base(provider)
        {
        }
    }
}
