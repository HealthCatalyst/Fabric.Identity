using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Fabric.Identity.API.Authorization;
using Fabric.Identity.API.Configuration;
using Fabric.Identity.API.Persistence;
using Fabric.Identity.UnitTests.Mocks;
using IdentityServer4.Models;
using Microsoft.AspNetCore.Authorization;
using Moq;
using Serilog;
using Xunit;
using Fabric.Platform.Shared.Configuration;

namespace Fabric.Identity.UnitTests
{

    public class RegistrationAuthorizationHandlerTests
    {
        public RegistrationAuthorizationHandlerTests()
        {
            _appConfiguration = new AppConfiguration
            {
                IssuerUri = "http://fabric.identity",
                RegistrationAdminGroup = "Domain Admins",
                IdentityServerConfidentialClientSettings = new IdentityServerConfidentialClientSettings
                {
                    Authority = "http://fabric.identity"
                }
            };
        }

        private readonly IAppConfiguration _appConfiguration;
        private readonly ILogger _logger = new Mock<ILogger>().Object;

        private RegistrationAuthorizationHandler GetRegistrationAuthorizationHandler(
            IClientManagementStore clientManagementStore)
        {
            return new RegistrationAuthorizationHandler(clientManagementStore, _appConfiguration, _logger);
        }

        private IClientManagementStore GetClientManagementStore(IList<Client> clients)
        {
            var mockClientManagementStore = new Mock<IClientManagementStore>();
            mockClientManagementStore.Setup(clientManagementStore => clientManagementStore.GetClientCount())
                .Returns(clients.Count);

            return mockClientManagementStore.Object;
        }

        [Fact]
        public void HandleRequirementAsync_ExceedsThreshold_Fails()
        {
            var clientManagementStore = GetClientManagementStore(new List<Client>
            {
                new Client
                {
                    ClientId = "test-client"
                }
            });
            var registrationAuthorizationHandler = GetRegistrationAuthorizationHandler(clientManagementStore);
            var requirement = new RegisteredClientThresholdRequirement(1);
            var context = new AuthorizationHandlerContext(new[] { requirement },
                new TestPrincipal(), null);
            var result = registrationAuthorizationHandler.HandleAsync(context);
            if (!result.IsCompleted)
            {
                result.RunSynchronously();
            }
            Assert.False(context.HasSucceeded);
        }

        [Fact]
        public void HandleRequirementsAsync_ExceedsThresholdButHasGroupClaim_Succeeds()
        {
            var clientManagementStore = GetClientManagementStore(new List<Client>
            {
                new Client
                {
                    ClientId = "test-client"
                }
            });
            var registrationAuthorizationHandler = GetRegistrationAuthorizationHandler(clientManagementStore);
            var requirement = new RegisteredClientThresholdRequirement(1);
            var roleClaim = new Claim(ClaimTypes.Role, _appConfiguration.RegistrationAdminGroup, "claim",
                _appConfiguration.IssuerUri);
            var context = new AuthorizationHandlerContext(new[] { requirement },
                new TestPrincipal(roleClaim), null);
            var result = registrationAuthorizationHandler.HandleAsync(context);
            if (!result.IsCompleted)
            {
                result.RunSynchronously();
            }
            Assert.True(context.HasSucceeded);
        }

        [Fact]
        public void HandleRequirementsAsync_ExceedsThresholdButHasScopeClaim_Succeeds()
        {
            var clientManagementStore = GetClientManagementStore(new List<Client>
            {
                new Client
                {
                    ClientId = "test-client"
                }
            });
            var registrationAuthorizationHandler = GetRegistrationAuthorizationHandler(clientManagementStore);
            var requirement = new RegisteredClientThresholdRequirement(1);
            var scopeClaim = new Claim(ClaimTypes.Role, _appConfiguration.RegistrationAdminGroup, "claim",
                _appConfiguration.IssuerUri);
            var context = new AuthorizationHandlerContext(new[] { requirement },
                new TestPrincipal(scopeClaim), null);
            var result = registrationAuthorizationHandler.HandleAsync(context);
            if (!result.IsCompleted)
            {
                result.RunSynchronously();
            }
            Assert.True(context.HasSucceeded);
        }

        [Fact]
        public void HandleRequirementsAsync_UnderThreshold_Succeeds()
        {
            var clientManagementStore = GetClientManagementStore(new List<Client>());
            var registrationAuthorizationHandler = GetRegistrationAuthorizationHandler(clientManagementStore);
            var requirement = new RegisteredClientThresholdRequirement(1);
            var context = new AuthorizationHandlerContext(new[] { requirement },
                new TestPrincipal(), null);
            var result = registrationAuthorizationHandler.HandleAsync(context);
            if (!result.IsCompleted)
            {
                result.RunSynchronously();
            }
            Assert.True(context.HasSucceeded);
        }
    }
}