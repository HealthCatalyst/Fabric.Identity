using System.Collections.Generic;
using IdentityServer4.Models;

namespace Fabric.Identity.API.Persistence
{
    public interface IDbBootstrapper
    {
        bool Setup();
        void AddResources(IEnumerable<IdentityResource> resources);
    }
}
