using System.Collections.Generic;
using IdentityModel;
using IdentityServer4.Models;

namespace Fabric.Identity.API
{
    public class Config
    {
        public static IEnumerable<IdentityResource> GetIdentityResources()
        {
            var fabricProfile = new IdentityResource(name: "fabric.profile", displayName: "Fabric Profile", claimTypes: new[] { "location", "allowedresource" });
            return new List<IdentityResource>
            {
                new IdentityResources.OpenId(),
                new IdentityResources.Profile(),
                new IdentityResources.Email(),
                new IdentityResources.Address(),
                fabricProfile
            };
        }

        public static IEnumerable<ApiResource> GetApiResources()
        {
            return new List<ApiResource>
            {
                new ApiResource
                {
                    Name = "patientapi",
                    UserClaims = { JwtClaimTypes.Name, JwtClaimTypes.Email, "allowedresource"},
                    Scopes = { new Scope("patientapi", "Patient API") }
                }
            };
        }
    }
}
