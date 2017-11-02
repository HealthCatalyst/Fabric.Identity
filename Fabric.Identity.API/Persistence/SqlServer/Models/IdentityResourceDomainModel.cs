using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Fabric.Identity.API.Persistence.SqlServer.Models
{
    public class IdentityResourceDomainModel : IdentityServer4.EntityFramework.Entities.IdentityResource, ITrackable, ISoftDelete
    {
        public DateTime CreatedDateTimeUtc { get; set; }
        public DateTime? ModifiedDateTimeUtc { get; set; }
        public string CreatedBy { get; set; }
        public string ModifiedBy { get; set; }
        public bool IsDeleted { get; set; }
    }
}
