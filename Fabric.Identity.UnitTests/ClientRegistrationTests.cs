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

namespace Fabric.Identity.UnitTests
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

        private static void GetDefaultController(out Mock<IDocumentDbService> mockDocumentDbService, out ClientController controller)
        {
            mockDocumentDbService = new Mock<IDocumentDbService>();
            var validator = new ClientValidator();
            var mockLogger = new Mock<ILogger>();

            controller = new ClientController(mockDocumentDbService.Object, validator, mockLogger.Object);
        }

        public static IEnumerable<object[]> GetInvalidClients()
        {
            yield return new object[] { new IS4.Client(), "Please specify an Id for this client" };
            yield return new object[] { new IS4.Client(), "Please specify a Name for this client" };
            yield return new object[] { new IS4.Client(), "Please specify at least one Allowed Scope for this client" };
            yield return new object[] { new IS4.Client(), "Please specify at least one Allowed Cors Origin when using implicit grant type" };

            yield return new object[]
            {
                new IS4.Client() { AllowOfflineAccess = true },
                "Client may not have Allow Offline Access when grant type is Implicit or ResourceOwnerPassword"
            };
            yield return new object[]
            {
                new IS4.Client() {AllowOfflineAccess = true, AllowedGrantTypes = new List<string>() { IS4.GrantType.ResourceOwnerPassword } },
                "Client may not have Allow Offline Access when grant type is Implicit or ResourceOwnerPassword"
            };

            yield return new object[]
            {
                new IS4.Client() { AllowedGrantTypes = new List<string>() { "Grant2" }},
                "Grant type not allowed"
            };

            yield return new object[]
            {
                new IS4.Client() { AllowedGrantTypes = new List<string>() { string.Empty }},
                "Grant type not allowed"
            };
        }

        [Theory]
        [InlineData(null, "is null")]
        [MemberData(nameof(GetInvalidClients))]
        public void TestCreateNewClient_Failures(IS4.Client client, string errorMessage)
        {
            GetDefaultController(out Mock<IDocumentDbService> mockDocumentDbService, out ClientController controller);

            var result = controller.Post(client);
            Assert.True(result is BadRequestObjectResult);
            Assert.True((result as BadRequestObjectResult).Value is Error);
            var error = (result as BadRequestObjectResult).Value as Error;

            if (error.Details != null)
            {
                Assert.True(error.Details.Any(e => e.Message.Contains(errorMessage)));
            }
            else
            {
                Assert.True(error.Message.Contains(errorMessage));
            }
        }

        [Fact]
        public void TestCreateNewClient_GeneratePassword()
        {
            GetDefaultController(out Mock<IDocumentDbService> mockDocumentDbService, out ClientController controller);

            var testClient = GetTestClient();
            var submittedSecret = testClient.ClientSecrets.First().Value;
            var result = controller.Post(testClient);
            Assert.True(result is CreatedAtRouteResult);
            Assert.True((result as CreatedAtRouteResult).Value is Client);

            var client = (result as CreatedAtRouteResult).Value as Client;
            Assert.NotNull(client.ClientSecret);
            Assert.NotEmpty(client.ClientSecret);

            // Password must be generated for the client.
            Assert.NotEqual(submittedSecret, client.ClientSecret);
        }

        [Fact]
        public void TestCreateNewClient_DBCall()
        {
            GetDefaultController(out Mock<IDocumentDbService> mockDocumentDbService, out ClientController controller);

            var testClient = GetTestClient();
            var result = controller.Post(testClient);
            Assert.True(result is CreatedAtRouteResult);
            Assert.True((result as CreatedAtRouteResult).Value is Client);

            var client = (result as CreatedAtRouteResult).Value as Client;
            Assert.Equal(testClient.ClientId, client.ClientId);
            Assert.Equal(testClient.ClientName, client.ClientName);

            mockDocumentDbService.Verify(m =>
                m.AddDocument<IS4.Client>(
                    It.Is<string>(id => id == testClient.ClientId),
                    It.IsAny<IS4.Client>()));
        }

        [Fact]
        public void TestGetClient_DBCall()
        {
            var testClient = GetTestClient();
            GetDefaultController(out Mock<IDocumentDbService> mockDocumentDbService, out ClientController controller);
            mockDocumentDbService.Setup(m =>
                m.GetDocument<IS4.Client>(It.Is<string>(id => id == testClient.ClientId))).Returns(Task.FromResult(testClient));

            var result = controller.Get(testClient.ClientId);
            Assert.True(result is OkObjectResult);
            Assert.True((result as OkObjectResult).Value is Client);

            var client = (result as OkObjectResult).Value as Client;
            Assert.Equal(testClient.ClientId, client.ClientId);
            Assert.Equal(testClient.ClientName, client.ClientName);

            // Password must never be returned
            Assert.Null(client.ClientSecret);
        }

        [Theory]
        [InlineData("1234")]
        [InlineData("")]
        [InlineData("randomId")]
        public void TestDeleteClient_DBCall(string clientId)
        {
            GetDefaultController(out Mock<IDocumentDbService> mockDocumentDbService, out ClientController controller);

            var result = controller.Delete(clientId);
            Assert.True(result is NoContentResult);

            mockDocumentDbService.Verify(m =>
                m.DeleteDocument<IS4.Client>(It.Is<string>(id => id == clientId)));
        }

        [Theory]
        [InlineData("1234")]
        [InlineData("randomId")]
        public void TestUpdateClient_DBCall(string clientId)
        {
            var secrets = new List<IS4.Secret>() { new IS4.Secret("secret") };

            GetDefaultController(out Mock<IDocumentDbService> mockDocumentDbService, out ClientController controller);

            mockDocumentDbService.Setup(m => m.GetDocument<IS4.Client>(It.Is<string>(id => id == clientId)))
                .Returns(Task.FromResult(new IS4.Client()
                {
                    ClientId = clientId,
                    ClientSecrets = secrets
                }));

            var testClient = GetTestClient();
            var result = controller.Put(clientId, testClient);
            Assert.True(result is NoContentResult);

            mockDocumentDbService.Verify(m =>
                m.UpdateDocument<IS4.Client>(It.Is<string>(id => id == clientId), It.IsAny<IS4.Client>()));

            mockDocumentDbService.Verify(m =>
                m.UpdateDocument<IS4.Client>(
                    It.Is<string>(id => id == clientId),
                    It.Is<IS4.Client>(c => c.ClientId == clientId && c.ClientSecrets.Equals(secrets))));
        }
    }
}