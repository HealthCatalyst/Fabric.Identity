using System.Collections.Generic;
using IdentityServer4.Models;

namespace Fabric.Identity.API.Events
{
    public class EntityCreatedAuditEvent<T> : EntityAuditEvent
    {
        public EntityCreatedAuditEvent(string username, string clientId, string subject, T entity)
            : base(username, clientId, subject, FabricIdentityConstants.AuditEventCategory, "Entity Created",
                FabricIdentityConstants.CustomEventIds.EntityCreatedAudit)
        {
            Entity = ObfuscateEntity(entity);
        }
       
        public T Entity { get; set; }
    }
}
