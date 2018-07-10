using System.ComponentModel;

namespace Fabric.Identity.API.Models
{
    public class IdentityResource : BaseResource
    {
        public bool Required { get; set; }
        public bool Emphasize { get; set; }

        [DefaultValue(true)]
        public bool ShowInDiscoveryDocument { get; set; } = true;
    }
}