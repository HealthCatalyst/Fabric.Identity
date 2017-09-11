using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Primitives;

namespace Fabric.Identity.API.Authorization
{
    public class ReadScopeRequirement : IAuthorizationRequirement
    {
        public string ReadScope { get; } = FabricIdentityConstants.IdentityReadScope;
    }
}
