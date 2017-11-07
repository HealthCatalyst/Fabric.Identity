using System;

namespace Fabric.Identity.API.Persistence.SqlServer.EntityModels
{
    public class PersistedGrant : ITrackable, ISoftDelete
    {
        public string Key { get; set; }
        public string ClientId { get; set; }
        public string Data { get; set; }
        public string SubjectId { get; set; }
        public string Type { get; set; }
        public DateTime CreationTime { get; set; }
        public DateTime Expiration { get; set; }


        public bool IsDeleted { get; set; }
        public DateTime CreatedDateTimeUtc { get; set; }
        public DateTime? ModifiedDateTimeUtc { get; set; }
        public string CreatedBy { get; set; }
        public string ModifiedBy { get; set; }
    }
}
