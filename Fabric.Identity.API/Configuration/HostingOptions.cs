namespace Fabric.Identity.API.Configuration
{
    public class HostingOptions
    {
        public bool UseIis { get; set; }
        public bool UseTestUsers { get; set; }
        public string StorageProvider { get; set; }
        public bool AllowUnsafeEval { get; set; }
    }
}
