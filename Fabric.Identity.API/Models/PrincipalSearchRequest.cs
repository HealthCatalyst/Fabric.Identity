using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Fabric.Identity.API.Models
{
    public class PrincipalSearchRequest
    {
        public string IdentityProvider { get; set; }
        public string TenantId { get; set; }
        public string SearchText { get; set; }
        public string Type { get; set; }
        public string SubjectId { get; set; }
        public string GroupName { get; set; }
        public string Email { get; set; }
    }
}
