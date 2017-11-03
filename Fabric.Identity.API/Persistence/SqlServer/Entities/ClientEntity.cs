using System;

namespace Fabric.Identity.API.Persistence.SqlServer.Entities
{
    public class ClientEntity : IdentityServer4.EntityFramework.Entities.Client, ISoftDelete, ITrackable
    {
        public bool IsDeleted { get; set; }
        public DateTime CreatedDateTimeUtc { get; set; }
        public DateTime? ModifiedDateTimeUtc { get; set; }
        public string CreatedBy { get; set; }
        public string ModifiedBy { get; set; }
    }
}
