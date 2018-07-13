using Microsoft.AspNetCore.Authorization;

namespace Fabric.Identity.API.Authorization
{
    public class ReadScopeRequirement : IAuthorizationRequirement, IHaveAuthorizationClaimType
    {
        public string ClaimType { get; } = FabricIdentityConstants.IdentityReadScope;
    }
}