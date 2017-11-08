using System;
using System.Collections.Generic;

namespace Fabric.Identity.API.Persistence.SqlServer.EntityModels
{
    public class User
    {
        public User()
        {
            Claims = new HashSet<IdentityClaim>();
            UserLogins = new HashSet<UserLogin>();            
        }

        public int Id { get; set; }
        public string SubjectId { get; set; }
        public string ProviderName { get; set; }
        public string FirstName { get; set; }
        public string MiddleName { get; set; }
        public string LastName { get; set; }
        public string Username { get; set; }
        public DateTime CreatedDateTimeUtc { get; set; }
        public DateTime? ModifiedDateTimeUtc { get; set; }
        public string CreatedBy { get; set; }
        public string ModifiedBy { get; set; }

        public virtual ICollection<IdentityClaim> Claims { get; set; }
        public virtual ICollection<UserLogin> UserLogins { get; set; }
    }
}
