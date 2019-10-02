using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Fabric.Identity.API;
using Fabric.Identity.API.Models;
using Newtonsoft.Json;
using Xunit;

namespace Fabric.Identity.IntegrationTests.ControllerTests.InMemory
{
    public class IdentityProvidersControllerTests : IntegrationTestsFixture
    {
        private readonly string _identityProvidersBaseUrl = "/api/identityProviders";

        public IdentityProvidersControllerTests(string provider = FabricIdentityConstants.StorageProviders.InMemory): base(provider)
        { }

        [Fact]
        public async Task IdentityProvidersController_Get_ReturnsProviders()
        {
            var httpClient = await HttpClient;
            var response = await httpClient.SendAsync(new HttpRequestMessage(new HttpMethod("GET"),
                $"{_identityProvidersBaseUrl}"));
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var externalProviders = JsonConvert.DeserializeObject<List<ExternalProviderApiModel>>(await response.Content.ReadAsStringAsync());
            Assert.Equal(1, externalProviders.Count);

        }
    }
}
