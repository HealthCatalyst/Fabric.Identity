using Microsoft.AspNetCore.Authorization;

namespace Fabric.Identity.API.Authorization
{
    public class SearchUserScopeRequirement : IAuthorizationRequirement, IHaveAuthorizationClaimType
    {
        public string ClaimType => FabricIdentityConstants.IdentitySearchUsersScope;
    }
}
