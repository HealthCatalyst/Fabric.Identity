using Fabric.Identity.API.Configuration;
using IdentityModel;
using Microsoft.AspNetCore.Authorization;
using Serilog;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Fabric.Identity.API.Authorization
{
    public abstract class BaseAuthorizationHandler<T> : AuthorizationHandler<T> 
        where T : IAuthorizationRequirement, IHaveAuthorizationClaimType
    {
        protected readonly ILogger _logger;
        protected readonly IAppConfiguration _appConfiguration;

        protected BaseAuthorizationHandler(IAppConfiguration appConfiguration, ILogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _appConfiguration = appConfiguration ?? throw new ArgumentNullException(nameof(appConfiguration));
        }

        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, T requirement)
        {
            if (HasRequiredScopeClaim(context.User, requirement.ClaimType))
            {
                _logger.Information($"User has required scope claim: {requirement.ClaimType}, authorization succeeded.");
                context.Succeed(requirement);
            }

            return Task.CompletedTask;
        }

        protected bool HasRequiredGroupClaim(ClaimsPrincipal user)
        {
            if (string.IsNullOrEmpty(_appConfiguration.RegistrationAdminGroup))
            {
                return false;
            }

            var hasGroupClaim = user.Claims.Any(
                c => (c.Type == ClaimTypes.Role || c.Type == FabricIdentityConstants.FabricClaimTypes.Groups)
                     && c.Value == _appConfiguration.RegistrationAdminGroup && c.Issuer.Equals(
                         _appConfiguration.IdentityServerConfidentialClientSettings.Authority,
                         StringComparison.OrdinalIgnoreCase));
            return hasGroupClaim;
        }

        protected bool HasRequiredScopeClaim(ClaimsPrincipal user, string claimType)
        {
            var hasScopeClaim = user.Claims.Any(
                c => c.Type == JwtClaimTypes.Scope && c.Value == claimType && c.Issuer.Equals(
                         this._appConfiguration.IdentityServerConfidentialClientSettings.Authority,
                         StringComparison.OrdinalIgnoreCase));
            return hasScopeClaim;
        }
    }
}
