using Fabric.Identity.API.Configuration;
using System.Security.Claims;
using Fabric.Identity.API;
using Fabric.Identity.API.Authorization;
using Fabric.Identity.UnitTests.Mocks;
using IdentityModel;
using Microsoft.AspNetCore.Authorization;
using Moq;
using Serilog;
using Xunit;
using Fabric.Platform.Shared.Configuration;

namespace Fabric.Identity.UnitTests
{

    public class ReadAuthorizationHandlerTests
    {
        private readonly IAppConfiguration _appConfiguration;
        private readonly ILogger _logger = new Mock<ILogger>().Object;

        public ReadAuthorizationHandlerTests()
        {
            _appConfiguration = new AppConfiguration
            {
                IssuerUri = "http://fabric.identity",
                IdentityServerConfidentialClientSettings = new IdentityServerConfidentialClientSettings
                {
                    Authority = "http://fabric.identity"
                }
            };
        }

        [Fact]
        public void HandleRequirementAsync_NoReadClaim_Fails()
        {
            var requirement = new ReadScopeRequirement();
            var readAuthorizationHandler = new ReadAuthorizationHandler(_appConfiguration, _logger);
            var context = new AuthorizationHandlerContext(new[] { requirement }, new TestPrincipal(), null);

            var result = readAuthorizationHandler.HandleAsync(context);
            if (!result.IsCompleted)
            {
                result.RunSynchronously();
            }

            Assert.False(context.HasSucceeded);
        }

        [Fact]
        public void HandleRequirementAsync_ReadClaim_Succeeds()
        {
            var requirement = new ReadScopeRequirement();
            var readAuthorizationHandler = new ReadAuthorizationHandler(_appConfiguration, _logger);
            var scopeClaim = new Claim(JwtClaimTypes.Scope, FabricIdentityConstants.IdentityReadScope, "claim",
                _appConfiguration.IssuerUri);
            var context = new AuthorizationHandlerContext(new[] { requirement }, new TestPrincipal(scopeClaim), null);
            var result = readAuthorizationHandler.HandleAsync(context);
            if (!result.IsCompleted)
            {
                result.RunSynchronously();
            }
            Assert.True(context.HasSucceeded);
        }
    }
}
