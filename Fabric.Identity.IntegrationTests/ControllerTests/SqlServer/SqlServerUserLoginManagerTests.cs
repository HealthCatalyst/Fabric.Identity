using Fabric.Identity.API;
using Fabric.Identity.API.Persistence.SqlServer.Stores;
using Fabric.Identity.API.Services;
using IdentityServer4.Services;
using Moq;

namespace Fabric.Identity.IntegrationTests.ControllerTests.SqlServer
{
    public class SqlServerUserLoginManagerTests : InMemory.UserLoginManagerTests
    {
        public SqlServerUserLoginManagerTests(string provider = FabricIdentityConstants.StorageProviders.SqlServer) 
            : base(provider)
        {
            var eventService = new Mock<IEventService>();
            var userResolverService = new Mock<IUserResolverService>();
            UserStore = new SqlServerUserStore(IdentityDbContext, eventService.Object, userResolverService.Object, new SerializationSettings());
        }
    }
}
