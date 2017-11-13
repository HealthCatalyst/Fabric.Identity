using System.Collections.Generic;

namespace Fabric.Identity.API.Models
{
    public class IdentityStatusModel
    {
        public IdentityStatusModel()
        {
            GrantsSupported = new List<string>();
            ScopesSupported = new List<string>();
        }
        public IEnumerable<string> GrantsSupported { get; set; }
        public IEnumerable<string> ScopesSupported { get; set; }
        public int ClientCount { get; set; }
        public string ErrorMessage { get; set; }
        public bool IsError { get; set; }
    }
}
