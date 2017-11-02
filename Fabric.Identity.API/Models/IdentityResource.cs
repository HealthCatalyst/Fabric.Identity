using System.Collections.Generic;

namespace Fabric.Identity.API.Models
{
    public class IdentityResource
    {
        public bool Enabled { get; set; } = true;
        public string Name { get; set; }
        public string DisplayName { get; set; }
        public string Description { get; set; }
        public bool Required { get; set; }
        public bool Emphasize { get; set; }
        public bool ShowInDiscoveryDocument { get; set; } = true;
        public ICollection<string> UserClaims { get; set; } = new HashSet<string>();
    }
}