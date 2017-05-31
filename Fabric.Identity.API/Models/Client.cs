using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using IdentityServer4.Models;

namespace Fabric.Identity.API.Models
{
    public class Client
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Secret { get; set; }
        public IEnumerable<string> AllowedScopes { get; set; }
        public IEnumerable<string> AllowedGrantTypes { get; set; }
        public IEnumerable<string> AllowedCorsOrigins { get; set; }
    }
}
