namespace Fabric.Identity.API.Configuration
{
    public class IdentityProviderSearchSettings
    {
        public string BaseUrl { get; set; }
        public string GetUserEndpoint { get; set; }
        public bool IsEnabled { get; set; }
    }
}
