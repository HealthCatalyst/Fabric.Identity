using System;
using System.Threading.Tasks;
using Fabric.Identity.API.Configuration;
using Fabric.Identity.API.Persistence;
using Fabric.Identity.API.Persistence.InMemory.Stores;
using Microsoft.AspNetCore.Authorization;
using Serilog;

namespace Fabric.Identity.API.Authorization
{
    public class RegistrationAuthorizationHandler : BaseAuthorizationHandler<RegisteredClientThresholdRequirement>
    {
        private readonly IClientManagementStore _clientManagementStore;

        public RegistrationAuthorizationHandler(IClientManagementStore clientManagementStore,
            IAppConfiguration appConfiguration, ILogger logger)
            : base(appConfiguration, logger)
        {
            _clientManagementStore = clientManagementStore ??
                                     throw new ArgumentNullException(nameof(clientManagementStore));
        }

        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context,
            RegisteredClientThresholdRequirement requirement)
        {
            var clientCount = GetClientDocumentCount();

            if (clientCount < requirement.RegisteredClientThreshold)
            {
                _logger.Information(
                    "Client count: {clientCount} below threshold: {registeredClientThreshold}, authorization succeeded.",
                    clientCount, requirement.RegisteredClientThreshold);
                context.Succeed(requirement);
                return Task.CompletedTask;
            }

            if (HasRequiredGroupClaim(context.User))
            {
                _logger.Information("User has required group claim, authorization succeeded.");
                context.Succeed(requirement);
                return Task.CompletedTask;
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
            return _clientManagementStore.GetClientCount();
        }
    }
}