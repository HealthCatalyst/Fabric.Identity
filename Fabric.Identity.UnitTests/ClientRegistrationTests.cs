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

        private static readonly Func<IS4.Client> GetOnlineTestClient = () => new IS4.Client()
        {
            ClientId = rand.Next().ToString(),
            ClientName = rand.Next().ToString(),
            RequireConsent = rand.Next() % 2 == 0,
            AllowOfflineAccess = false,
            AllowedScopes = new List<string>() { rand.Next().ToString() },
            AllowedGrantTypes = new List<string>() { IS4.GrantType.Implicit },
            ClientSecrets = new List<IS4.Secret>() { new IS4.Secret(rand.Next().ToString()) },
            RedirectUris = new List<string>() { rand.Next().ToString() },
            AllowedCorsOrigins = new List<string>() { rand.Next().ToString() },
            PostLogoutRedirectUris = new List<string>() { rand.Next().ToString() },
        };

        private static readonly Func<IS4.Client> GetOfflineTestClient = () => new IS4.Client()
        {
            ClientId = rand.Next().ToString(),
            ClientName = rand.Next().ToString(),
            RequireConsent = rand.Next() % 2 == 0,
            AllowOfflineAccess = true,
            AllowedScopes = new List<string>() { rand.Next().ToString() },
            AllowedGrantTypes = new List<string>() { IS4.GrantType.ClientCredentials },
            ClientSecrets = new List<IS4.Secret>() { new IS4.Secret(rand.Next().ToString()) },
            RedirectUris = new List<string>() { rand.Next().ToString() },
            AllowedCorsOrigins = new List<string>() { rand.Next().ToString() },
            PostLogoutRedirectUris = new List<string>() { rand.Next().ToString() },
        };

        private static readonly Func<IS4.Client> GetTestClient = rand.Next() % 2 == 0 ? GetOfflineTestClient : GetOnlineTestClient;

        /// <summary>
        /// A collection of valid clients.
        /// </summary>
        private static IEnumerable<object[]> GetValidClients() => Enumerable.Range(1, 6).Select(_ => new object[] { GetTestClient() });

        /// <summary>
        /// A collection of invalid clients, i.e., clients that won't pass validation.
        /// </summary>
        public static IEnumerable<object[]> GetInvalidClients()
        {
            yield return new object[] { new IS4.Client(), "Please specify an Id for this client" };
            yield return new object[] { new IS4.Client(), "Please specify a Name for this client" };
            yield return new object[] { new IS4.Client(), "Please specify at least one Allowed Scope for this client" };
            yield return new object[] { new IS4.Client(), "Please specify at least one Allowed Cors Origin when using Implicit grant type" };

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

        private static void GetDefaultController(out Mock<IDocumentDbService> mockDocumentDbService, out ClientController controller)
        {
            mockDocumentDbService = new Mock<IDocumentDbService>();
            var validator = new ClientValidator();
            var mockLogger = new Mock<ILogger>();

            controller = new ClientController(mockDocumentDbService.Object, validator, mockLogger.Object);
        }

        [Theory]
        [InlineData(null, "is nonexistent or malformed")]
        [MemberData(nameof(GetInvalidClients))]
        public void TestCreateNewClient_Failures(IS4.Client client, string errorMessage)
        {
            GetDefaultController(out Mock<IDocumentDbService> mockDocumentDbService, out ClientController controller);

            var result = controller.Post(client?.ToClientViewModel());
            Assert.True(result is BadRequestObjectResult);
            Assert.True((result as BadRequestObjectResult).Value is Error);
            var error = (result as BadRequestObjectResult).Value as Error;

            if (error.Details != null)
            {
                Assert.True(error.Details.Any(e => e.Message.Contains(errorMessage)), $"error message not found in error stiring. returned error message: {String.Join(",", error.Details.Select(e => e.Message)) }");
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
            var result = controller.Post(testClient.ToClientViewModel());
            Assert.True(result is CreatedAtActionResult);
            Assert.True((result as CreatedAtActionResult).Value is Client);

            var client = (result as CreatedAtActionResult).Value as Client;
            Assert.NotNull(client.ClientSecret);
            Assert.NotEmpty(client.ClientSecret);

            // Password must be generated for the client.
            Assert.NotEqual(submittedSecret, client.ClientSecret);
        }

        [Theory]
        [MemberData(nameof(GetValidClients))]
        public void TestCreateNewClient_DBCall(IS4.Client testClient)
        {
            GetDefaultController(out Mock<IDocumentDbService> mockDocumentDbService, out ClientController controller);

            var result = controller.Post(testClient.ToClientViewModel());
            Assert.True(result is CreatedAtActionResult);
            Assert.True((result as CreatedAtActionResult).Value is Client);

            var client = (result as CreatedAtActionResult).Value as Client;
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
            var result = controller.Put(clientId, testClient.ToClientViewModel());
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