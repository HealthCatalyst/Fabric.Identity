using System;

namespace Fabric.Identity.API.Persistence.SqlServer.EntityModels
{
    public class UserLogin 
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string ClientId { get; set; }
        public DateTime LoginDate { get; set; }
        
        public virtual User User { get; set; }
    }
}
