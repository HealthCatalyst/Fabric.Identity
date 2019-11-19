using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Fabric.Identity.API.Models
{
    public class FabricGroupApiModel
    {
        public string ExternalIdentifier { get; set; }
        public string GroupName { get; set; }
        public string TenantId { get; set; }
        public string TenantAlias { get; set; }
        public string IdentityProvider { get; set; }
        public FabricIdentityEnums.PrincipalType PrincipalType { get; set; }
    }
}
