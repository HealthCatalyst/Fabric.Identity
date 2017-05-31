using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fabric.Identity.API.Management;
using Fabric.Identity.API.Models;
using Fabric.Identity.API.Services;
using Fabric.Identity.API.Validation;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Serilog;
using Xunit;
using IS4 = IdentityServer4.Models;

namespace Fabric.Identity.UnitTests.Clients
{
    public class ClientRegistrationTests
    {
        private static readonly Random rand = new Random(DateTime.Now.Millisecond);
        private static readonly Func<IS4.Client> GetTestClient = () => new IS4.Client()
        {
            ClientId = rand.Next().ToString(),
            ClientName = "ClientName",
            AllowedScopes = new List<string>() { "scope" },
            AllowedGrantTypes = new List<string>() { IS4.GrantType.AuthorizationCode },
            ClientSecrets = new List<IS4.Secret>() { new IS4.Secret(rand.Next().ToString()) }
        };

        [Fact]
        public void TestCreateNewClient_Failures()
        {
            var mockDocumentDbService = new Mock<IDocumentDbService>();
            var mockLogger = new Mock<ILogger>();
            var validator = new ClientValidator();

            var controller = new ClientController(mockDocumentDbService.Object, validator, mockLogger.Object);

            // Null client object
            var result = controller.Post(null);
            Assert.True(result is BadRequestObjectResult);
            Assert.True((result as BadRequestObjectResult).Value is Error);
            Assert.True(((result as BadRequestObjectResult).Value as Error).Message.Contains("is null"));

            // Empty client ID
            result = controller.Post(new IS4.Client());
            Assert.True(result is BadRequestObjectResult);
            Assert.True((result as BadRequestObjectResult).Value is Error);

            var error = (result as BadRequestObjectResult).Value as Error;
            Assert.True(error.Details.Length > 0);
            Assert.True(error.Details.Any(e => e.Message.Contains("Please specify an Id for this client")));
            Assert.True(error.Details.Any(e => e.Message.Contains("Please specify a Name for this client")));
            Assert.True(error.Details.Any(e => e.Message.Contains("Please specify at least one Allowed Scope for this client")));
            Assert.True(error.Details.Any(e => e.Message.Contains("Please specify at least one Allowed Cors Origin when using implicit grant type")));
        }

        [Fact]
        public void TestCreateNewClient_GeneratePassword()
        {
            var mockDocumentDbService = new Mock<IDocumentDbService>();
            var validator = new ClientValidator();
            var mockLogger = new Mock<ILogger>();

            var controller = new ClientController(mockDocumentDbService.Object, validator, mockLogger.Object);

            var testClient = GetTestClient();
            var submittedSecret = testClient.ClientSecrets.First().Value;
            var result = controller.Post(testClient);
            Assert.True(result is CreatedAtRouteResult);
            Assert.True((result as CreatedAtRouteResult).Value is Client);

            var client = (result as CreatedAtRouteResult).Value as Client;
            Assert.NotNull(client.Secret);
            Assert.NotEmpty(client.Secret);

            // Password must be generated for the client.
            Assert.NotEqual(submittedSecret, client.Secret);
        }

        [Fact]
        public void TestCreateNewClient_DBCall()
        {
            var mockDocumentDbService = new Mock<IDocumentDbService>();
            var validator = new ClientValidator();
            var mockLogger = new Mock<ILogger>();

            var controller = new ClientController(mockDocumentDbService.Object, validator, mockLogger.Object);

            var testClient = GetTestClient();
            var result = controller.Post(testClient);
            Assert.True(result is CreatedAtRouteResult);
            Assert.True((result as CreatedAtRouteResult).Value is Client);

            var client = (result as CreatedAtRouteResult).Value as Client;
            Assert.Equal(testClient.ClientId, client.Id);
            Assert.Equal(testClient.ClientName, client.Name);

            mockDocumentDbService.Verify(m =>
                m.AddDocument<IS4.Client>(
                    It.Is<string>(id => id == testClient.ClientId),
                    It.IsAny<IS4.Client>()));
        }

        [Fact]
        public void TestGetClient_DBCall()
        {
            var mockDocumentDbService = new Mock<IDocumentDbService>();
            var validator = new ClientValidator();
            var mockLogger = new Mock<ILogger>();

            var testClient = GetTestClient();
            mockDocumentDbService.Setup(m =>
                m.GetDocument<IS4.Client>(It.Is<string>(id => id == testClient.ClientId))).Returns(Task.FromResult(testClient));

            var controller = new ClientController(mockDocumentDbService.Object, validator, mockLogger.Object);

            var result = controller.Get(testClient.ClientId);
            Assert.True(result is OkObjectResult);
            Assert.True((result as OkObjectResult).Value is Client);

            var client = (result as OkObjectResult).Value as Client;
            Assert.Equal(testClient.ClientId, client.Id);
            Assert.Equal(testClient.ClientName, client.Name);

            // Password must never be returned
            Assert.Null(client.Secret);
        }

        [Theory]
        [InlineData("1234")]
        [InlineData("")]
        [InlineData("randomId")]
        public void TestDeleteClient_DBCall(string clientId)
        {
            var mockDocumentDbService = new Mock<IDocumentDbService>();
            var validator = new ClientValidator();
            var mockLogger = new Mock<ILogger>();

            var controller = new ClientController(mockDocumentDbService.Object, validator, mockLogger.Object);

            var result = controller.Delete(clientId);
            Assert.True(result is NoContentResult);

            mockDocumentDbService.Verify(m =>
                m.DeleteDocument<IS4.Client>(It.Is<string>(id => id == clientId)));
        }

        [Theory]
        [InlineData("1234")]
        [InlineData("")]
        [InlineData("randomId")]
        public void TestUpdateClient_DBCall(string clientId)
        {
            var mockDocumentDbService = new Mock<IDocumentDbService>();
            var validator = new ClientValidator();
            var mockLogger = new Mock<ILogger>();

            var controller = new ClientController(mockDocumentDbService.Object, validator, mockLogger.Object);

            var testClient = GetTestClient();
            var result = controller.Put(clientId, testClient);
            Assert.True(result is NoContentResult);

            mockDocumentDbService.Verify(m =>
                m.UpdateDocument<IS4.Client>(It.Is<string>(id => id == clientId), It.IsAny<IS4.Client>()));
        }
    }
}