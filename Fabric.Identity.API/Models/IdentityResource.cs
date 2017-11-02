namespace Fabric.Identity.API.Models
{
    public class IdentityResource : BaseResource
    {
        public bool Required { get; set; }
        public bool Emphasize { get; set; }
        public bool ShowInDiscoveryDocument { get; set; } = true;
    }
}