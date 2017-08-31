using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Fabric.Identity.API.Events
{
    public class EntityReadAuditEvent<T> : EntityAuditEvent<T>
    {
        public EntityReadAuditEvent(string username, string clientId, string subject, string documentId, ISerializationSettings serializationSettings)
            : base(username,
                  clientId,
                  subject,
                  documentId,
                  FabricIdentityConstants.AuditEventCategory,
                  FabricIdentityConstants.CustomEventNames.EntityReadAudit,
                  FabricIdentityConstants.CustomEventIds.EntityReadAudit,
                  serializationSettings)
        {
        }
    }
}
