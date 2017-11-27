using System;

namespace Fabric.Identity.API.Persistence.SqlServer.EntityModels
{
    public class ClientSecret
    {
        public int Id { get; set; }
        public int ClientId { get; set; }
        public string Description { get; set; }
        public DateTime? Expiration { get; set; }
        public string Type { get; set; }
        public string Value { get; set; }

        public virtual Client Client { get; set; }
    }
}
