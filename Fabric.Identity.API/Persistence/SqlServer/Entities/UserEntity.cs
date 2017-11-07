using System;
using System.Collections.Generic;
using Fabric.Identity.API.Persistence.SqlServer.EntityModels;
using IdentityClaim = IdentityServer4.EntityFramework.Entities.IdentityClaim;

namespace Fabric.Identity.API.Persistence.SqlServer.Entities
{
    public class UserEntity : ITrackable, ISoftDelete
    {
        public string SubjectId { get; set; }
        public string Username { get; set; }
        public string FirstName { get; set; }
        public string MiddleName { get; set; }
        public string LastName { get; set; }
        public string ProviderName { get; set; }

        public ICollection<IdentityClaim> Claims { get; set; }
        public ICollection<UserLoginEntity> LastLoginDatesByClient { get; set; }

        public DateTime CreatedDateTimeUtc { get; set; }
        public DateTime? ModifiedDateTimeUtc { get; set; }
        public string CreatedBy { get; set; }
        public string ModifiedBy { get; set; }
        public bool IsDeleted { get; set; }        
    }
}
