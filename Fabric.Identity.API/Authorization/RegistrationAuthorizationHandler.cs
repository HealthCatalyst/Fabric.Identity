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
    public class RegistrationAuthorizationHandler : BaseAuthorizationHandler<RegisteredClientThresholdRequirement>
    {
        private readonly IDocumentDbService _documentDbService;
      
        public RegistrationAuthorizationHandler(IDocumentDbService documentDbService, IAppConfiguration appConfiguration, ILogger logger)
            : base(appConfiguration, logger)
        {
            _documentDbService = documentDbService ?? throw new ArgumentNullException(nameof(documentDbService));           
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

        
    }
}
