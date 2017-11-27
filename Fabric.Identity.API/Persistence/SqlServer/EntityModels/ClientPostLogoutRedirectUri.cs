namespace Fabric.Identity.API.Persistence.SqlServer.EntityModels
{
    public class ClientPostLogoutRedirectUri
    {
        public int Id { get; set; }
        public int ClientId { get; set; }
        public string PostLogoutRedirectUri { get; set; }

        public virtual Client Client { get; set; }
    }
}
