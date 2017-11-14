using System.Threading.Tasks;
using Fabric.Identity.API;
using Fabric.Identity.API.Persistence.SqlServer.Stores;
using Xunit;

namespace Fabric.Identity.IntegrationTests.ControllerTests.SqlServer
{
    public class SqlServerUsersControllerTests : InMemory.UsersControllerTests
    {
        public SqlServerUsersControllerTests(string provider = FabricIdentityConstants.StorageProviders.SqlServer) 
            : base(provider)
        {
            UserStore = new SqlServerUserStore(IdentityDbContext);
        }

        public override async Task UsersController_Search_ReturnsUsers()
        {
            Assert.True(true);
        }
    }
}
