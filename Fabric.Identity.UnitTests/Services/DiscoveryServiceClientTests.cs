using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

using Fabric.Identity.API.Models;
using Fabric.Identity.API.Services;
using Fabric.Identity.UnitTests.Mocks;

using Moq;

using Newtonsoft.Json;

using Xunit;

namespace Fabric.Identity.UnitTests.Services
{

    public class DiscoveryServiceClientTests
    {
        private Mock<MockHttpHandler> _mockHttpHandler;
        public DiscoveryServiceClientTests()
        {
            _mockHttpHandler = new Mock<MockHttpHandler> { CallBase = true };
        }
        [Fact]
        public async Task GetService_ShouldReturn_ValidServiceAsync()
        {
            var expectedIdentityServiceModel =
                new DiscoveryServiceApiModel
                    {
                        ServiceUrl = "http://localhost/IdentityProviderSearchService",
                        ServiceName = "IdentityProviderSearchService",
                        Version = 1,
                        DiscoveryType = "Service"
                    };

            var discoveryBaseUrl = "http://localhost/DiscoveryService/v1/";
            var discoverySearchUrl =
                $"{discoveryBaseUrl}Services(ServiceName='{expectedIdentityServiceModel.ServiceName}', Version={expectedIdentityServiceModel.Version})";


            _mockHttpHandler.Setup(httpHandler => httpHandler.Send(It.IsAny<HttpRequestMessage>()))
                .Returns((HttpRequestMessage requestMessage) =>
                    {
                        if (requestMessage.RequestUri.ToString()
                            .Equals(discoverySearchUrl, StringComparison.OrdinalIgnoreCase))
                        {
                            return new HttpResponseMessage
                                       {
                                           StatusCode = HttpStatusCode.OK,
                                           Content = new StringContent(
                                               JsonConvert.SerializeObject(
                                                   expectedIdentityServiceModel))
                                       };
                        }
                        return new HttpResponseMessage
                                   {
                                       StatusCode = HttpStatusCode.BadRequest
                                   };
                    });

            var discoveryServiceClient = new DiscoveryServiceClient("http://localhost/DiscoveryService/v1", _mockHttpHandler.Object);
            var serviceRegistration = await discoveryServiceClient.GetServiceAsync(
                                           expectedIdentityServiceModel.ServiceName,
                                           expectedIdentityServiceModel.Version);

            Assert.Equal(expectedIdentityServiceModel.ServiceUrl, serviceRegistration.ServiceUrl);
        }

        [Fact]
        public async Task GetService_ShouldThrow_ForNonSuccessCodeAsync()
        {
            _mockHttpHandler.Setup(httpHandler => httpHandler.Send(It.IsAny<HttpRequestMessage>()))
                .Returns((HttpRequestMessage requestMessage) =>
                    new HttpResponseMessage
                        {
                            StatusCode = HttpStatusCode.BadRequest
                        });
            var discoveryServiceClient = new DiscoveryServiceClient("http://localhost/DiscoveryService/v1/", _mockHttpHandler.Object);
            await Assert.ThrowsAsync<HttpRequestException>(() => discoveryServiceClient.GetServiceAsync("Identity", 1));
        }
    }
}
