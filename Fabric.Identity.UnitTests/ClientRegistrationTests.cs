using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fabric.Identity.API.Management;
using Fabric.Identity.API.Models;
using Fabric.Identity.API.Validation;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Serilog;
using Xunit;
using IS4 = IdentityServer4.Models;
using Fabric.Identity.API.Persistence;

namespace Fabric.Identity.UnitTests
{
    public class ClientRegistrationTests
    {
        private static readonly Random Random = new Random(DateTime.Now.Millisecond);

        private static readonly Func<Client> GetOnlineTestClient = () => new Client
        {
            ClientId = Random.Next().ToString(),
            ClientName = Random.Next().ToString(),
            RequireConsent = Random.Next() % 2 == 0,
            AllowOfflineAccess = false,
            AllowedScopes = new List<string> { Random.Next().ToString() },
            AllowedGrantTypes = new List<string> { IS4.GrantType.Implicit },
            ClientSecret = Random.Next().ToString(),
            RedirectUris = new List<string> { Random.Next().ToString() },
            AllowedCorsOrigins = new List<string> { Random.Next().ToString() },
            PostLogoutRedirectUris = new List<string> { Random.Next().ToString() },
        };

        private static readonly Func<Client> GetOfflineTestClient = () => new Client
        {
            ClientId = Random.Next().ToString(),
            ClientName = Random.Next().ToString(),
            RequireConsent = Random.Next() % 2 == 0,
            AllowOfflineAccess = true,
            AllowedScopes = new List<string> { Random.Next().ToString() },
            AllowedGrantTypes = new List<string> { IS4.GrantType.ClientCredentials },
            ClientSecret = Random.Next().ToString(),
            RedirectUris = new List<string> { Random.Next().ToString() },
            AllowedCorsOrigins = new List<string> { Random.Next().ToString() },
            PostLogoutRedirectUris = new List<string> { Random.Next().ToString() },
        };

        private static readonly Func<Client> GetTestClient = Random.Next() % 2 == 0 ? GetOfflineTestClient : GetOnlineTestClient;

        /// <summary>
        /// A collection of valid clients.
        /// </summary>
        private static IEnumerable<object[]> GetValidClients() => Enumerable.Range(1, 6).Select(_ => new object[] { GetTestClient() });

        /// <summary>
        /// A collection of invalid clients, i.e., clients that won't pass validation.
        /// </summary>
        public static IEnumerable<object[]> GetInvalidClients()
        {
            yield return new object[] {null, "is nonexistent or malformed"};
            yield return new object[] {new Client { AllowedGrantTypes = IS4.GrantTypes.Implicit }, "Please specify an Id for this client"};
            yield return new object[] {new Client { AllowedGrantTypes = IS4.GrantTypes.Implicit }, "Please specify a Name for this client"};
            yield return new object[] {new Client { AllowedGrantTypes = IS4.GrantTypes.Implicit }, "Please specify at least one Allowed Scope for this client"};
            yield return new object[]
                {new Client { AllowedGrantTypes = IS4.GrantTypes.Implicit }, "Please specify at least one Allowed Cors Origin when using Implicit grant type"};

            yield return new object[]
            {
                new Client {AllowOfflineAccess = true, AllowedGrantTypes = IS4.GrantTypes.Implicit},
                "Client may not have Allow Offline Access when grant type is Implicit or ResourceOwnerPassword"
            };
            yield return new object[]
            {
                new Client
                {
                    AllowOfflineAccess = true,
                    AllowedGrantTypes = new List<string> {IS4.GrantType.ResourceOwnerPassword}
                },
                "Client may not have Allow Offline Access when grant type is Implicit or ResourceOwnerPassword"
            };

            yield return new object[]
            {
                new Client {AllowedGrantTypes = new List<string> {"Grant2"}},
                "Grant type not allowed"
            };

            yield return new object[]
            {
                new Client {AllowedGrantTypes = new List<string> {string.Empty}},
                "Grant type not allowed"
            };

            yield return new object[]
            {
                new Client {AllowedGrantTypes = new List<string> {"hybrid","implicit" }},
                "Grant types list cannot contain both implicit and hybrid"
            };
        }

        private static void GetDefaultController(out Mock<IClientManagementStore> mockClientManagementStore, out ClientController controller)
        {
            mockClientManagementStore = new Mock<IClientManagementStore>();
            var validator = new ClientValidator(mockClientManagementStore.Object);
            var mockLogger = new Mock<ILogger>();
            controller = new ClientController(mockClientManagementStore.Object, validator, mockLogger.Object);
        }

        [Theory]
        [MemberData(nameof(GetInvalidClients))]
        public void TestCreateNewClient_Failures(Client client, string errorMessage)
        {
            GetDefaultController(out Mock<IClientManagementStore> mockClientManagementStore, out ClientController controller);

            var result = controller.Post(client);

            var badRequestObjectResult = result as ObjectResult;
            Assert.NotNull(badRequestObjectResult);

            var error = badRequestObjectResult.Value as Error;
            Assert.NotNull(error);

            if (error.Details != null)
            {
                Assert.True(error.Details.Any(e => e.Message.Contains(errorMessage)), $"error message not found in error string. returned error message: {string.Join(",", error.Details.Select(e => e.Message)) }");
            }
            else
            {
                Assert.True(error.Message.Contains(errorMessage));
            }
        }

        [Fact]
        public void TestCreateNewClient_GeneratePassword()
        {
            GetDefaultController(out Mock<IClientManagementStore> mockClientManagementStore, out ClientController controller);

            var testClient = GetTestClient();
            var submittedSecret = testClient.ClientSecret;
            var result = controller.Post(testClient);

            var createdAtActionResult = result as CreatedAtActionResult;
            Assert.NotNull(createdAtActionResult);

            var clientResult = createdAtActionResult.Value as Client;
            Assert.NotNull(clientResult);

            Assert.NotNull(clientResult.ClientSecret);
            Assert.NotEmpty(clientResult.ClientSecret);

            // Password must be generated for the client.
            Assert.NotEqual(submittedSecret, clientResult.ClientSecret);
        }

        [Theory]
        [MemberData(nameof(GetValidClients))]
        public void TestCreateNewClient_DBCall(Client testClient)
        {
            GetDefaultController(out Mock<IClientManagementStore> mockClientManagementStore, out ClientController controller);

            var result = controller.Post(testClient);
            var createdAtActionResult = result as CreatedAtActionResult;
            Assert.NotNull(createdAtActionResult);

            var clientResult = createdAtActionResult.Value as Client;
            Assert.NotNull(clientResult);

            Assert.Equal(testClient.ClientId, clientResult.ClientId);
            Assert.Equal(testClient.ClientName, clientResult.ClientName);

            mockClientManagementStore.Verify(m =>
                m.AddClient(It.IsAny<IS4.Client>()));
        }

        [Fact]
        public void TestGetClient_DBCall()
        {
            var testClient = GetTestClient();
            GetDefaultController(out Mock<IClientManagementStore> mockClientManagementStore, out ClientController controller);
            mockClientManagementStore.Setup(m =>
                m.FindClientByIdAsync(It.Is<string>(id => id == testClient.ClientId))).Returns(Task.FromResult(testClient.ToIs4ClientModel()));

            var result = controller.Get(testClient.ClientId);

            var okObjectResult = result as OkObjectResult;
            Assert.NotNull(okObjectResult);

            var clientResult = okObjectResult.Value as Client;
            Assert.NotNull(clientResult);

            Assert.Equal(testClient.ClientId, clientResult.ClientId);
            Assert.Equal(testClient.ClientName, clientResult.ClientName);

            // Password must never be returned
            Assert.Null(clientResult.ClientSecret);
        }

        [Theory]
        [InlineData("1234")]
        [InlineData("randomId")]
        public void TestUpdateClient_DBCall(string clientId)
        {
            var secrets = new List<IS4.Secret>() { new IS4.Secret("secret") };

            GetDefaultController(out Mock<IClientManagementStore> mockClientManagementStore, out ClientController controller);

            mockClientManagementStore.Setup(m => m.FindClientByIdAsync(It.Is<string>(id => id == clientId)))
                .Returns(Task.FromResult(new IS4.Client
                {
                    ClientId = clientId,
                    ClientSecrets = secrets
                }));

            var testClient = GetTestClient();
            testClient.ClientId = clientId;
            var result = controller.Put(clientId, testClient);
            Assert.True(result is NoContentResult);

            mockClientManagementStore.Verify(m =>
                m.UpdateClient(It.Is<string>(id => id == clientId), It.IsAny<IS4.Client>()));

            mockClientManagementStore.Verify(m =>
                m.UpdateClient(
                    It.Is<string>(id => id == clientId),
                    It.Is<IS4.Client>(c => c.ClientId == clientId && c.ClientSecrets.Equals(secrets))));
        }
    }
}