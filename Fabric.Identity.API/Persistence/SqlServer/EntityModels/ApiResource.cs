using System;
using System.Collections.Generic;

namespace Fabric.Identity.API.Persistence.SqlServer.EntityModels
{
    public class ApiResource : ITrackable, ISoftDelete
    {
        public ApiResource()
        {
            ApiClaims = new HashSet<ApiClaim>();
            ApiScopes = new HashSet<ApiScope>();
            ApiSecrets = new HashSet<ApiSecret>();
        }

        public int Id { get; set; }
        public string Description { get; set; }
        public string DisplayName { get; set; }
        public bool Enabled { get; set; } = true;
        public string Name { get; set; }
        public DateTime CreatedDateTimeUtc { get; set; }
        public DateTime? ModifiedDateTimeUtc { get; set; }
        public string CreatedBy { get; set; }
        public string ModifiedBy { get; set; }
        public bool IsDeleted { get; set; }

        public virtual ICollection<ApiClaim> ApiClaims { get; set; }
        public virtual ICollection<ApiScope> ApiScopes { get; set; }
        public virtual ICollection<ApiSecret> ApiSecrets { get; set; }
    }
}
