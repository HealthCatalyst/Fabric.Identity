using System.Collections.Generic;

namespace Fabric.Identity.API.Persistence.SqlServer.EntityModels
{
    public partial class ApiScope : ISoftDelete
    {
        public ApiScope()
        {
            ApiScopeClaims = new HashSet<ApiScopeClaim>();
        }

        public int Id { get; set; }
        public int ApiResourceId { get; set; }
        public string Description { get; set; }
        public string DisplayName { get; set; }
        public bool Emphasize { get; set; }
        public string Name { get; set; }
        public bool Required { get; set; }
        public bool ShowInDiscoveryDocument { get; set; }
        public bool IsDeleted { get; set; }

        public virtual ICollection<ApiScopeClaim> ApiScopeClaims { get; set; }
        public virtual ApiResource ApiResource { get; set; }
    }
}
