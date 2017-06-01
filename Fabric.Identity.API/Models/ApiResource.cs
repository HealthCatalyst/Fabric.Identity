using System.Collections.Generic;

namespace Fabric.Identity.API.Models
{
    public class ApiResource
    {
        public bool Enabled { get; set; }
        public string Name { get; set; }
        public string DisplayName { get; set; }
        public string Description { get; set; }
        public string ApiSecret { get; set; }
        public ICollection<string> UserClaims { get; set; }
        public ICollection<Scope> Scopes { get; set; }
    }

    public class Scope
    {
        public string Name { get; set; }
        public string DisplayName { get; set; }
        public string Description { get; set; }
        public bool Required { get; set; }
        public bool Emphasize { get; set; }
        public bool ShowInDiscoveryDocument { get; set; }
        public ICollection<string> UserClaims { get; set; }
    }
}