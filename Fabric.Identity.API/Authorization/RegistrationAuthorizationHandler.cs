using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Fabric.Identity.API.Configuration;
using Fabric.Identity.API.Services;
using IdentityModel;
using Microsoft.AspNetCore.Authorization;
using Serilog;

namespace Fabric.Identity.API.Authorization
{
    public class RegistrationAuthorizationHandler : AuthorizationHandler<RegisteredClientThresholdRequirement>
    {
        private readonly IDocumentDbService _documentDbService;
        private readonly IAppConfiguration _appConfiguration;
        private readonly ILogger _logger;
        public RegistrationAuthorizationHandler(IDocumentDbService documentDbService, IAppConfiguration appConfiguration, ILogger logger)
        {
            _documentDbService = documentDbService ?? throw new ArgumentNullException(nameof(documentDbService));
            _appConfiguration = appConfiguration ?? throw new ArgumentNullException(nameof(appConfiguration));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }
        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, RegisteredClientThresholdRequirement requirement)
        {
            var clientCount = GetClientDocumentCount();

            if (clientCount < requirement.RegisteredClientThreshold)
            {
                _logger.Information("Client count: {clientCount} below threshold: {registeredClientThreshold}, authorization succeeded.", clientCount, requirement.RegisteredClientThreshold);
                context.Succeed(requirement);
                return Task.CompletedTask;
            }

            if (HasRequiredGroupClaim(context.User))
            {
                _logger.Information("User has required group claim, authorization succeeded.");
                context.Succeed(requirement);
                return  Task.CompletedTask;
            }

            if (HasRequiredScopeClaim(context.User, requirement.ClaimType))
            {
                _logger.Information("User has required scope claim, authorization succeeded.");
                context.Succeed(requirement);
                return Task.CompletedTask;
            }
            
            return Task.CompletedTask;
        }

        private int GetClientDocumentCount()
        {
            return _documentDbService.GetDocumentCount(FabricIdentityConstants.DocumentTypes.ClientDocumentType)
                .Result;
        }

        private bool HasRequiredGroupClaim(ClaimsPrincipal user)
        {
            if (string.IsNullOrEmpty(_appConfiguration.RegistrationAdminGroup))
            {
                return false;
            }

            var hasGroupClaim = user.Claims.Any(c => (c.Type == ClaimTypes.Role ||
                                                      c.Type == FabricIdentityConstants.FabricClaimTypes.Groups) &&
                                                     c.Value == _appConfiguration.RegistrationAdminGroup &&
                                                     c.Issuer == _appConfiguration.IssuerUri);
            return hasGroupClaim;
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
