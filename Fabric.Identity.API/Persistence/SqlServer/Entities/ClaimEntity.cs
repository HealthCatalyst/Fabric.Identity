using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Fabric.Identity.API.Persistence.SqlServer.Entities
{
    public class ClaimEntity
    {
        public int Id { get; set; }
        public string Type { get; set; }
    }
}
