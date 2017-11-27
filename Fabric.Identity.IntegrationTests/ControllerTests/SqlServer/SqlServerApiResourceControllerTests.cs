using Fabric.Identity.API;

namespace Fabric.Identity.IntegrationTests.ControllerTests.SqlServer
{
    public class SqlServerApiResourceControllerTests : InMemory.ApiResourceControllerTests
    {
        public SqlServerApiResourceControllerTests(string provider = FabricIdentityConstants.StorageProviders.SqlServer) 
            : base(provider)
        {
        }
    }
}
