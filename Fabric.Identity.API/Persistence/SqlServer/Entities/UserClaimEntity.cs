using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Fabric.Identity.API.Persistence.SqlServer.Entities
{
    public class UserClaimEntity
    {
        public int UserId { get; set; }
        public string Type { get; set; }
        public IdentityResourceEntity IdentityResource { get; set; }
    }
}
