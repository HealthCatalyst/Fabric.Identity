using System;

namespace Fabric.Identity.API.Models
{
    public class UserLogin
    {        
        public string ClientId { get; set; }
        public DateTime LoginDate { get; set; }
    }
}
