using Microsoft.AspNetCore.Authorization;

namespace Fabric.Identity.API.Authorization
{
    public class RegisteredClientThresholdRequirement : IAuthorizationRequirement
    {
        public int RegisteredClientThreshold { get; }

        public string ClaimType => FabricIdentityConstants.IdentityRegistrationScope;

        public RegisteredClientThresholdRequirement(int registeredClientThreshold)
        {
            RegisteredClientThreshold = registeredClientThreshold;
        }
    }
}
