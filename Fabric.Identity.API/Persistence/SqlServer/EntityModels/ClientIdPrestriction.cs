namespace Fabric.Identity.API.Persistence.SqlServer.EntityModels
{
    public class ClientIdpRestriction
    {
        public int Id { get; set; }
        public int ClientId { get; set; }
        public string Provider { get; set; }

        public virtual Client Client { get; set; }
    }
}
