namespace Fabric.Identity.API.Configuration
{
    public class AzureActiveDirectorySettings
    {
        public string DisplayName { get; set; }
        public string Authority { get; set; }
        public string ClaimsIssuer { get; set; }
        public string ClientId { get; set; }
        public string ClientSecret { get; set; }
        public string[] Scope { get; set; }
        public string[] IssuerWhiteList { get; set; }
    }
}
