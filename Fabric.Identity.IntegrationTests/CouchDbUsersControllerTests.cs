using Fabric.Identity.API;
using Fabric.Identity.API.Persistence.CouchDb.Stores;
using Moq;
using Serilog;

namespace Fabric.Identity.IntegrationTests
{
    public class CouchDbUsersControllerTests : UsersControllerTests
    {
        public CouchDbUsersControllerTests() : base(FabricIdentityConstants.StorageProviders.CouchDb)
        {
            UserStore = new CouchDbUserStore(CouchDbService, new Mock<ILogger>().Object);
        }
    }
}
