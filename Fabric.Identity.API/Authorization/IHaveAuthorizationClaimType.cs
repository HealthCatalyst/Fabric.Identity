using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Fabric.Identity.API.Authorization
{
    public interface IHaveAuthorizationClaimType
    {
        string ClaimType { get; }
    }
}
