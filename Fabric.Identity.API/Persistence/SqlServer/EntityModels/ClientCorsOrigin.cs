namespace Fabric.Identity.API.Persistence.SqlServer.EntityModels
{
    public class ClientCorsOrigin
    {
        public int Id { get; set; }
        public int ClientId { get; set; }
        public string Origin { get; set; }

        public virtual Client Client { get; set; }
    }
}
