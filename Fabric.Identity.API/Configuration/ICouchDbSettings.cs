namespace Fabric.Identity.API.Configuration
{
    public interface ICouchDbSettings
    {
        string DatabaseName { get; set; }
        string Server { get; set; }
    }
}