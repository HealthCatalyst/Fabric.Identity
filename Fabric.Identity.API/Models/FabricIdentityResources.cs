using IdentityModel;
using IdentityServer4.Models;

namespace Fabric.Identity.API.Models
{
    public static class FabricIdentityResources
    {
        public class FabricProfile : IdentityResource
        {
            public FabricProfile()
            {
                Name = "fabric.profile";
                DisplayName = "Fabric Profile";
                Emphasize = true;
                UserClaims = new[] {JwtClaimTypes.Role, FabricIdentityConstants.FabricClaimTypes.Groups};
            }
        }
    }
}
