using System.Collections.Generic;
using System.Security.Claims;

namespace Fabric.Identity.API.Models
{
    public class User
    {
        public string SubjectId { get; set; }
        public string Username { get; set; }
        public string ProviderName { get; set; }
        public ICollection<Claim> Claims { get; set; }
    }
}
