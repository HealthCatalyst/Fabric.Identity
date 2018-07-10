using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Fabric.Identity.API.Models
{
    public class ApiResource : BaseResource
    {
        public string ApiSecret { get; set; }

        [Required]
        public ICollection<Scope> Scopes { get; set; }
    }

    public class Scope
    {
        [Required]
        public string Name { get; set; }

        public string DisplayName { get; set; }
        public string Description { get; set; }
        public bool Required { get; set; }
        public bool Emphasize { get; set; }

        [DefaultValue(true)]
        public bool ShowInDiscoveryDocument { get; set; } = true;

        public ICollection<string> UserClaims { get; set; } = new HashSet<string>();
    }
}