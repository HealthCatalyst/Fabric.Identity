using System;

namespace Fabric.Identity.API.Persistence.SqlServer.Entities
{
    public class ApiResourceEntity : IdentityServer4.EntityFramework.Entities.ApiResource, ITrackable, ISoftDelete
    {
        public DateTime CreatedDateTimeUtc { get; set; }
        public DateTime? ModifiedDateTimeUtc { get; set; }
        public string CreatedBy { get; set; }
        public string ModifiedBy { get; set; }
        public bool IsDeleted { get; set; }
    }
}