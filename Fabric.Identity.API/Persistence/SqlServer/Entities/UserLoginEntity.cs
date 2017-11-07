using System;

namespace Fabric.Identity.API.Persistence.SqlServer.Entities
{
    public class UserLoginEntity : ITrackable, ISoftDelete
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int ClientId { get; set; }
        public DateTime LoginDate { get; set; }


        public DateTime CreatedDateTimeUtc { get; set; }
        public DateTime? ModifiedDateTimeUtc { get; set; }
        public string CreatedBy { get; set; }
        public string ModifiedBy { get; set; }
        public bool IsDeleted { get; set; }
    }
}
