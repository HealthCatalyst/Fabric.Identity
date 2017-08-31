using System.Collections.Generic;
using IdentityServer4.Models;

namespace Fabric.Identity.API.Events
{
    public class EntityCreatedAuditEvent<T> : EntityAuditEvent<T>
    {
        public EntityCreatedAuditEvent(string username, string clientId, string subject, string documentId, T entity, ISerializationSettings serializationSettings)
            : base(username, 
                  clientId, 
                  subject, 
                  documentId, 
                  FabricIdentityConstants.AuditEventCategory, 
                  FabricIdentityConstants.CustomEventNames.EntityCreatedAudit,
                  FabricIdentityConstants.CustomEventIds.EntityCreatedAudit, entity, serializationSettings)
        {
        }
       
    }
}
