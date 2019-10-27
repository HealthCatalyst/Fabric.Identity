using System.Net.Http;
using System.Threading.Tasks;
using Fabric.Identity.API;
using IdentityModel.Client;
using Xunit;

namespace Fabric.Identity.IntegrationTests.ControllerTests.InMemory
{
    public class DiscoveryDocumentTests : IntegrationTestsFixture
    {
        public DiscoveryDocumentTests(string provider = FabricIdentityConstants.StorageProviders.InMemory) : base(
            provider)
        {
            
        }

        [Fact]
        public async Task DiscoveryUrlShownInDiscoveryDocument_Success()
        {
            var discoveryDocumentRequest = new DiscoveryDocumentRequest
            {
                Address = IdentityServerUrl
            };

            var discoClient = new HttpClient(IdentityTestServer.CreateHandler());

            var discoDocument = await discoClient.GetDiscoveryDocumentAsync(discoveryDocumentRequest);
            var discoveryUri = discoDocument.TryGetValue("discovery_uri");
            Assert.NotNull(discoveryUri);
        }
    }
}
