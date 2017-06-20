namespace Fabric.Identity.API.Configuration
{
    public class HostingOptions
    {
        public bool UseIis { get; set; }
        public bool UseInMemoryStores { get; set; }

        public bool UseTestUsers { get; set; }
    }
}
