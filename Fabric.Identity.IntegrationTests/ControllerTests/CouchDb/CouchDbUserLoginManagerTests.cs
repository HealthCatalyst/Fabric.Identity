using Fabric.Identity.API;
using Fabric.Identity.API.Persistence.CouchDb.Stores;
using Moq;
using Serilog;

namespace Fabric.Identity.IntegrationTests.ControllerTests.CouchDb
{
    public class CouchDbUserLoginManagerTests //: InMemory.UserLoginManagerTests
    {
        public CouchDbUserLoginManagerTests(string provider = FabricIdentityConstants.StorageProviders.CouchDb) 
            //: base(provider)
        {
            //UserStore = new CouchDbUserStore(CouchDbService, new Mock<ILogger>().Object);
        }
    }
}
