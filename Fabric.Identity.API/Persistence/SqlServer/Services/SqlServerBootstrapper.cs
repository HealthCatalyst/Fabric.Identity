using System.Collections.Generic;
using IdentityServer4.Models;

namespace Fabric.Identity.API.Persistence.SqlServer.Services
{
    public class SqlServerBootstrapper : IDbBootstrapper
    {
        public bool Setup()
        {
            return true;
        }

        public void AddResources(IEnumerable<IdentityResource> resources)
        {
            
        }
    }
}