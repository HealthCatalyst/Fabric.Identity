using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Text;
using Fabric.Identity.API;
using Fabric.Identity.API.Authorization;
using Fabric.Identity.API.Configuration;
using Fabric.Identity.UnitTests.Mocks;
using IdentityModel;
using Microsoft.AspNetCore.Authorization;
using Moq;
using Serilog;
using Xunit;
using Fabric.Platform.Shared.Configuration;

namespace Fabric.Identity.UnitTests
{

    public class SearchUsersAuthorizationHandlerTests
    {
        private readonly IAppConfiguration _appConfiguration;
        private readonly ILogger _logger = new Mock<ILogger>().Object;

        public SearchUsersAuthorizationHandlerTests()
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
        public void HandleRequirementAsync_NoSearchUsersClaim_Fails()
        {
            var requirement = new SearchUserScopeRequirement();
            var searchUserAuthorizationHandler = new SearchUserAuthorizationHandler(_appConfiguration, _logger);
            var context = new AuthorizationHandlerContext(new[] { requirement }, new TestPrincipal(), null);

            var result = searchUserAuthorizationHandler.HandleAsync(context);
            if (!result.IsCompleted)
            {
                result.RunSynchronously();
            }

            Assert.False(context.HasSucceeded);
        }

        [Fact]
        public void HandleRequirementAsync_SearchUsersClaim_Succeeds()
        {
            var requirement = new SearchUserScopeRequirement();
            var searchUserAuthorizationHandler = new SearchUserAuthorizationHandler(_appConfiguration, _logger);
            var scopeClaim = new Claim(JwtClaimTypes.Scope, FabricIdentityConstants.IdentitySearchUsersScope, "claim",
                _appConfiguration.IssuerUri);
            var context = new AuthorizationHandlerContext(new[] { requirement }, new TestPrincipal(scopeClaim), null);
            var result = searchUserAuthorizationHandler.HandleAsync(context);
            if (!result.IsCompleted)
            {
                result.RunSynchronously();
            }
            Assert.True(context.HasSucceeded);
        }
    }
}
