using System.Collections.Generic;

namespace Fabric.Identity.API.Models
{
    public class UserSearchParameter
    {
        public string ClientId { get; set; }
        public IEnumerable<string> UserIds { get; set; }
    }
}
