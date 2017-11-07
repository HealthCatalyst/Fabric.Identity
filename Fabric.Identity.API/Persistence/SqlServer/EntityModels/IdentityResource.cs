using System;
using System.Collections.Generic;
using Fabric.Identity.API.Persistence.SqlServer.Entities;

namespace Fabric.Identity.API.Persistence.SqlServer.EntityModels
{
    public partial class IdentityResource : ITrackable, ISoftDelete
    {
        public IdentityResource()
        {
            IdentityClaims = new HashSet<IdentityClaim>();
        }

        public int Id { get; set; }
        public string Description { get; set; }
        public string DisplayName { get; set; }
        public bool Emphasize { get; set; }
        public bool Enabled { get; set; } = true;
        public string Name { get; set; }
        public bool Required { get; set; }
        public bool ShowInDiscoveryDocument { get; set; } = true;
        public DateTime CreatedDateTimeUtc { get; set; }
        public DateTime? ModifiedDateTimeUtc { get; set; }
        public string CreatedBy { get; set; }
        public string ModifiedBy { get; set; }
        public bool IsDeleted { get; set; }

        public virtual ICollection<IdentityClaim> IdentityClaims { get; set; }
    }
}
