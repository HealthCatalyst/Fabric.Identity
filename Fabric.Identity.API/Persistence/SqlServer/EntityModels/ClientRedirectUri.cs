namespace Fabric.Identity.API.Persistence.SqlServer.EntityModels
{
    public class ClientRedirectUri
    {
        public int Id { get; set; }
        public int ClientId { get; set; }
        public string RedirectUri { get; set; }

        public virtual Client Client { get; set; }
    }
}
