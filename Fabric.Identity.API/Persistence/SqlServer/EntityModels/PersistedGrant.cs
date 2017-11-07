using System;

namespace Fabric.Identity.API.Persistence.SqlServer.EntityModels
{
    public class PersistedGrant
    {
        public string Key { get; set; }
        public string ClientId { get; set; }
        public string Data { get; set; }
        public string SubjectId { get; set; }
        public string Type { get; set; }
        public DateTime CreationTime { get; set; }
        public DateTime Expiration { get; set; }
    }
}
