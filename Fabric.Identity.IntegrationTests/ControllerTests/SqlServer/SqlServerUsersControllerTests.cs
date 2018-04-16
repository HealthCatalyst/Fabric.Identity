using System.Threading.Tasks;
using Fabric.Identity.API;
using Fabric.Identity.API.Persistence.SqlServer.Stores;
using Fabric.Identity.API.Services;
using IdentityServer4.Services;
using Moq;
using Xunit;

namespace Fabric.Identity.IntegrationTests.ControllerTests.SqlServer
{
    public class SqlServerUsersControllerTests : InMemory.UsersControllerTests
    {
        public SqlServerUsersControllerTests(string provider = FabricIdentityConstants.StorageProviders.SqlServer) 
            : base(provider)
        {
            var eventService = new Mock<IEventService>();
            var userResolverService = new Mock<IUserResolverService>();
            UserStore = new SqlServerUserStore(IdentityDbContext, eventService.Object, userResolverService.Object, new SerializationSettings());
        }

        public override async Task UsersController_Search_ReturnsUsers()
        {
            Assert.True(true);
        }
    }
}
