using System.Collections.Generic;
using Fabric.Identity.API.Models;
using IS4 = IdentityServer4.Models;

namespace Fabric.Identity.API
{
    public static class Config
    {
        public static IEnumerable<IS4.IdentityResource> GetIdentityResources()
        {
            return new List<IS4.IdentityResource>
            {
                new IS4.IdentityResources.OpenId(),
                new IS4.IdentityResources.Profile(),
                new IS4.IdentityResources.Email(),
                new IS4.IdentityResources.Address(),
                new FabricIdentityResources.FabricProfile()
            };
        }
        
    }
}
