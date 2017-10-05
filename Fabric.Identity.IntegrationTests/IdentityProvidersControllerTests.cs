using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Fabric.Identity.API.Models;
using Newtonsoft.Json;
using Xunit;

namespace Fabric.Identity.IntegrationTests
{
    public class IdentityProvidersControllerTests : IntegrationTestsFixture
    {
        private readonly string _identityProvidersBaseUrl = "/api/identityProviders";
        [Fact]
        public async Task UsersController_Search_InvalidIdentityProvider_ReturnsBadRequest()
        {
            var response = await HttpClient.SendAsync(new HttpRequestMessage(new HttpMethod("GET"),
                $"{_identityProvidersBaseUrl}"));
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var externalProviders = JsonConvert.DeserializeObject<List<ExternalProviderApiModel>>(await response.Content.ReadAsStringAsync());
            Assert.Equal(1, externalProviders.Count);

        }
    }
}
