using System.Collections.Generic;

namespace Fabric.Identity.API.Models
{
    public class Client
    {
        public string ClientId { get; set; }
        public string ClientName { get; set; }
        public string ClientSecret { get; set; }
        public IEnumerable<string> AllowedScopes { get; set; }
        public IEnumerable<string> AllowedGrantTypes { get; set; }
        public IEnumerable<string> AllowedCorsOrigins { get; set; }
        public IEnumerable<string> RedirectUris { get; set; }
        public IEnumerable<string> PostLogoutRedirectUris { get; set; }
        public bool AllowOfflineAccess { get; set; }
        public bool RequireConsent { get; set; }
    }
}