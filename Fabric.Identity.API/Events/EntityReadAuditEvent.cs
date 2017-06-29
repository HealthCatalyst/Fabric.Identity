using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Fabric.Identity.API.Events
{
    public class EntityReadAuditEvent<T> : EntityAuditEvent
    {
        public EntityReadAuditEvent(string username, string clientId, string subject, string documentId)
            : base(username, clientId, subject, FabricIdentityConstants.AuditEventCategory, "Entity Read",
                FabricIdentityConstants.CustomEventIds.EntityReadAudit)
        {
            DocumentId = documentId;
            TypeName = typeof(T).FullName;
        }

        public string DocumentId { get; set; }
        public string TypeName { get; set; }
    }
}
