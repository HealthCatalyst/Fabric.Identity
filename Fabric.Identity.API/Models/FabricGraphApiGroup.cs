using Microsoft.Graph;

namespace Fabric.Identity.API.Models
{
    public class FabricGraphApiGroup
    {
        public FabricGraphApiGroup(Group group)
        {
            Group = group;
        }

        public Group Group { get; set; }

        public string TenantId { get; set; }

        public string TenantAlias { get; set; }
    }
}
