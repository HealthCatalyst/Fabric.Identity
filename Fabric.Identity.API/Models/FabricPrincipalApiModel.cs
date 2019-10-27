using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Fabric.Identity.API.Models
{
    public class FabricPrincipalApiModel
    {
        public string SubjectId { get; set; }
        public string UserPrincipal { get; set; }
        public string FirstName { get; set; }
        public string MiddleName { get; set; }
        public string LastName { get; set; }
        public string ExternalIdentifier { get; set; }
        public string DisplayName { get; set; }
        public string TenantId { get; set; }
        public string TenantAlias { get; set; }
        public string IdentityProvider { get; set; }
        public string PrincipalType { get; set; }
        public string IdentityProviderUserPrincipalName { get; set; }
        public string Email { get; set; }
    }
}
