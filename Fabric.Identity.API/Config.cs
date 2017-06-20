using System.Collections.Generic;
using Fabric.Identity.API.Models;
using IdentityServer4.Models;

namespace Fabric.Identity.API
{
    public static class Config
    {
        public static IEnumerable<IdentityResource> GetIdentityResources()
        {
            return new List<IdentityResource>
            {
                new IdentityResources.OpenId(),
                new IdentityResources.Profile(),
                new IdentityResources.Email(),
                new IdentityResources.Address(),
                new FabricIdentityResources.FabricProfile()
            };
        }
        
    }
}
