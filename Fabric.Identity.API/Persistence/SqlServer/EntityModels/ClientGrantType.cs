namespace Fabric.Identity.API.Persistence.SqlServer.EntityModels
{
    public class ClientGrantType
    {
        public int Id { get; set; }
        public int ClientId { get; set; }
        public string GrantType { get; set; }

        public virtual Client Client { get; set; }
    }
}
