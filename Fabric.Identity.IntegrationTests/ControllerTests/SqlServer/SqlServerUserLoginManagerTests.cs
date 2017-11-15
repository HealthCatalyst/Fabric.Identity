using Fabric.Identity.API;
using Fabric.Identity.API.Persistence.SqlServer.Stores;

namespace Fabric.Identity.IntegrationTests.ControllerTests.SqlServer
{
    public class SqlServerUserLoginManagerTests : InMemory.UserLoginManagerTests
    {
        public SqlServerUserLoginManagerTests(string provider = FabricIdentityConstants.StorageProviders.SqlServer) 
            : base(provider)
        {
            UserStore = new SqlServerUserStore(IdentityDbContext);
        }
    }
}
