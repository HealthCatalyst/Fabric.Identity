using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Collections.Generic;

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
        private string _discoveryBaseUrl = "http://localhost/DiscoveryService/v1/";
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
            
            var discoverySearchUrl =
                $"{_discoveryBaseUrl}Services?$filter=ServiceName eq '{expectedIdentityServiceModel.ServiceName}' and Version eq {expectedIdentityServiceModel.Version}";


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
                                                   new DiscoveryServiceResponseModel
                                                       {
                                                           Context =
                                                               $"{_discoveryBaseUrl}$metadata#Services",
                                                           Value =
                                                               new
                                                                   List<DiscoveryServiceApiModel>
                                                                       {
                                                                           expectedIdentityServiceModel
                                                                       }
                                                       }))
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

        [Fact]
        public async Task GetService_ShouldThrow_WhenMultipleServicesReturnedAsync()
        {
            _mockHttpHandler.Setup(httpHandler => httpHandler.Send(It.IsAny<HttpRequestMessage>()))
                .Returns(
                    (HttpRequestMessage requestMessage) => new HttpResponseMessage
                                                               {
                                                                   StatusCode = HttpStatusCode.OK,
                                                                   RequestMessage = requestMessage,
                                                                   Content = new StringContent(
                                                                       JsonConvert.SerializeObject(
                                                                           new
                                                                               DiscoveryServiceResponseModel
                                                                                   {
                                                                                       Context
                                                                                           =
                                                                                           $"{_discoveryBaseUrl}$metadata#Services",
                                                                                       Value
                                                                                           =
                                                                                           new
                                                                                               List<DiscoveryServiceApiModel>
                                                                                                   {
                                                                                                       new
                                                                                                           DiscoveryServiceApiModel(),
                                                                                                       new
                                                                                                           DiscoveryServiceApiModel()
                                                                                                   }
                                                                                   }))
                                                               });

            var discoveryServiceClient = new DiscoveryServiceClient("http://localhost/DiscoveryService/v1/", _mockHttpHandler.Object);
            await Assert.ThrowsAsync<InvalidOperationException>(() => discoveryServiceClient.GetServiceAsync("Identity", 1));
        }

        [Fact]
        public async Task GetService_ShouldThrow_WhenNoServiceIsReturned()
        {
            _mockHttpHandler.Setup(httpHandler => httpHandler.Send(It.IsAny<HttpRequestMessage>()))
                .Returns(
                    (HttpRequestMessage requestMessage) => new HttpResponseMessage
                                                               {
                                                                   StatusCode = HttpStatusCode.OK,
                                                                   RequestMessage = requestMessage,
                                                                   Content = new StringContent(
                                                                       JsonConvert.SerializeObject(
                                                                           new
                                                                               DiscoveryServiceResponseModel
                                                                                   {
                                                                                       Context = $"{_discoveryBaseUrl}$metadata#Services",
                                                                                       Value = new List<DiscoveryServiceApiModel>()
                                                                                   }))
                                                               });
            var discoveryServiceClient = new DiscoveryServiceClient("http://localhost/DiscoveryService/v1/", _mockHttpHandler.Object);
            await Assert.ThrowsAsync<InvalidOperationException>(() => discoveryServiceClient.GetServiceAsync("Identity", 1));
        }

        [Fact]
        public async Task GetService_ShouldThrow_WhenEmptyValueIsReturnedAsync()
        {
            _mockHttpHandler.Setup(httpHandler => httpHandler.Send(It.IsAny<HttpRequestMessage>()))
                .Returns(
                    (HttpRequestMessage requestMessage) => new HttpResponseMessage
                                                               {
                                                                   StatusCode = HttpStatusCode.OK,
                                                                   RequestMessage = requestMessage,
                                                                   Content = new StringContent(
                                                                       JsonConvert.SerializeObject(
                                                                           new
                                                                               DiscoveryServiceResponseModel
                                                                                   {
                                                                                       Context = $"{_discoveryBaseUrl}$metadata#Services"
                                                                                   }))
                                                               });
            var discoveryServiceClient = new DiscoveryServiceClient("http://localhost/DiscoveryService/v1/", _mockHttpHandler.Object);
            await Assert.ThrowsAsync<InvalidOperationException>(() => discoveryServiceClient.GetServiceAsync("Identity", 1));
        }

        [Fact]
        public async Task GetService_ShouldThrow_WhenInvalidJsonIsReturnedAsync()
        {
            _mockHttpHandler.Setup(httpHandler => httpHandler.Send(It.IsAny<HttpRequestMessage>()))
                .Returns(
                    (HttpRequestMessage requestMessage) => new HttpResponseMessage
                                                               {
                                                                   StatusCode = HttpStatusCode.OK,
                                                                   RequestMessage = requestMessage,
                                                                   Content = new StringContent("this is not json")
                                                               });
            var discoveryServiceClient = new DiscoveryServiceClient("http://localhost/DiscoveryService/v1/", _mockHttpHandler.Object);
            await Assert.ThrowsAsync<InvalidOperationException>(() => discoveryServiceClient.GetServiceAsync("Identity", 1));
        }
    }
}
