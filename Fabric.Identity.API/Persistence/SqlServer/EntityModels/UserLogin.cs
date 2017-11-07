using System;
using System.Collections.Generic;

namespace Fabric.Identity.API.Persistence.SqlServer.EntityModels
{
    public partial class UserLogin
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int ClientId { get; set; }
        public DateTime LoginDate { get; set; }

        public virtual Client Client { get; set; }
        public virtual User User { get; set; }
    }
}
