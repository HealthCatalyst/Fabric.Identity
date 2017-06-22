using Microsoft.AspNetCore.Authorization;

namespace Fabric.Identity.API.Authorization
{
    public class RegisteredClientThresholdRequirement : IAuthorizationRequirement
    {
        public int RegisteredClientThreshold { get; }

        public RegisteredClientThresholdRequirement(int registeredClientThreshold)
        {
            RegisteredClientThreshold = registeredClientThreshold;
        }
    }
}
