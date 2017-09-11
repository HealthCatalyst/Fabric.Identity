using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Fabric.Identity.API.Configuration;
using IdentityModel;
using Microsoft.AspNetCore.Authorization;
using Serilog;

namespace Fabric.Identity.API.Authorization
{
    public class ReadAuthorizationHandler : AuthorizationHandler<ReadScopeRequirement>
    {
        private readonly ILogger _logger;
        private readonly IAppConfiguration _appConfiguration;

        public ReadAuthorizationHandler(IAppConfiguration appConfiguration, ILogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _appConfiguration = appConfiguration ?? throw new ArgumentNullException(nameof(appConfiguration));
        }

        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, ReadScopeRequirement requirement)
        {
            //ensure the logged in client has the read scope claim

            if (HasRequiredScopeClaim(context.User, requirement.ReadScope))
            {
                _logger.Information($"User has required scope claim: {requirement.ReadScope}, authorization succeeded.");
                context.Succeed(requirement);                
            }

            return Task.CompletedTask;
        }

        private bool HasRequiredScopeClaim(ClaimsPrincipal user, string claimType)
        {
            var hasScopeClaim = user.Claims.Any(c => c.Type == JwtClaimTypes.Scope &&
                                                     c.Value == claimType &&
                                                     c.Issuer == _appConfiguration.IssuerUri);
            return hasScopeClaim;
        }
    }
}
