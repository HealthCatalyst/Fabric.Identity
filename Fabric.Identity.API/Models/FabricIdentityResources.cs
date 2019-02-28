using IdentityModel;
using IS4 = IdentityServer4.Models;

namespace Fabric.Identity.API.Models
{
    public static class FabricIdentityResources
    {
        public class FabricProfile : IS4.IdentityResource
        {
            public FabricProfile()
            {
                Name = "fabric.profile";
                DisplayName = "Fabric Profile";
                Emphasize = true;
                UserClaims = new[] {JwtClaimTypes.Role, FabricIdentityConstants.FabricClaimTypes.Groups, FabricIdentityConstants.PublicClaimTypes.UserPrincipalName};
            }
        }
    }
}