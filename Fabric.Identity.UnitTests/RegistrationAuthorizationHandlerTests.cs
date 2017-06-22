using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Fabric.Identity.API;
using Fabric.Identity.API.Authorization;
using Fabric.Identity.API.Configuration;
using Fabric.Identity.API.Services;
using Fabric.Identity.UnitTests.Mocks;
using IdentityModel;
using IdentityServer4.Models;
using Microsoft.AspNetCore.Authorization;
using Serilog;
using Xunit;
using Moq;

namespace Fabric.Identity.UnitTests
{
    public class RegistrationAuthorizationHandlerTests
    {
        private readonly IAppConfiguration _appConfiguration;
        private readonly ILogger _logger = new Mock<ILogger>().Object;

        public RegistrationAuthorizationHandlerTests()
        {
            _appConfiguration = new AppConfiguration
            {
                IssuerUri = "http://fabric.identity",
                RegistrationAdminGroup = "Domain Admins"
            };
        }
        [Fact]
        public void HandleRequirementAsync_ExceedsThreshold_Fails()
        {
            var documentDbService = GetDocumentDbService(new List<Client>()
            {
                new Client
                {
                    ClientId = "test-client"
                }
            });
            var registrationAuthorizationHandler = GetRegistrationAuthorizationHandler(documentDbService);
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
            var documentDbService = GetDocumentDbService(new List<Client>()
            {
                new Client
                {
                    ClientId = "test-client"
                }
            });
            var registrationAuthorizationHandler = GetRegistrationAuthorizationHandler(documentDbService);
            var requirement = new RegisteredClientThresholdRequirement(1);
            var roleClaim = new Claim(ClaimTypes.Role, _appConfiguration.RegistrationAdminGroup, "claim", _appConfiguration.IssuerUri);
            var context = new AuthorizationHandlerContext(new[] {requirement},
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
            var documentDbService = GetDocumentDbService(new List<Client>()
            {
                new Client
                {
                    ClientId = "test-client"
                }
            });
            var registrationAuthorizationHandler = GetRegistrationAuthorizationHandler(documentDbService);
            var requirement = new RegisteredClientThresholdRequirement(1);
            var scopeClaim = new Claim(ClaimTypes.Role, _appConfiguration.RegistrationAdminGroup, "claim", _appConfiguration.IssuerUri);
            var context = new AuthorizationHandlerContext(new[] {requirement},
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
            var documentDbService = GetDocumentDbService(new List<Client>());
            var registrationAuthorizationHandler = GetRegistrationAuthorizationHandler(documentDbService);
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

        private RegistrationAuthorizationHandler GetRegistrationAuthorizationHandler(IDocumentDbService documentDbService)
        {
            return new RegistrationAuthorizationHandler(documentDbService, _appConfiguration, _logger);
        }

        private IDocumentDbService GetDocumentDbService(IList<Client> clients)
        {
            var mockDocumentDbService = new Mock<IDocumentDbService>();
            mockDocumentDbService
                .Setup(documentDbService => documentDbService.GetDocumentCount(It.IsAny<string>()))
                .Returns(Task.FromResult(clients.Count));
            return mockDocumentDbService.Object;
        }
    }
}
