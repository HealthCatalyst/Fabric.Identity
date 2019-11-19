using Microsoft.Graph;

namespace Fabric.Identity.API.Models
{
    public class FabricGraphApiUser
    {
        public Microsoft.Graph.User User { get; set; }

        public FabricGraphApiUser(Microsoft.Graph.User user)
        {
            this.User = user;
        }
        public string IdentityProvider { get; set; }
        public string TenantId { get; set; }
        public string TenantAlias { get; set; }
    }
}
