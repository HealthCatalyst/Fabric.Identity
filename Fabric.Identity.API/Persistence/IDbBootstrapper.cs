using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using IdentityServer4.Models;

namespace Fabric.Identity.API.Persistence
{
    public interface IDbBootstrapper
    {
        bool Setup();
        void AddResources(IEnumerable<IdentityResource> resources);
    }
}
