namespace Fabric.Identity.API.Persistence.SqlServer.EntityModels
{
    public class ClientClaim
    {
        public int Id { get; set; }
        public int ClientId { get; set; }
        public string Type { get; set; }
        public string Value { get; set; }

        public virtual Client Client { get; set; }
    }
}
