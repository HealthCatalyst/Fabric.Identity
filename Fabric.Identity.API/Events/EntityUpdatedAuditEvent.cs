namespace Fabric.Identity.API.Events
{
    public class EntityUpdatedAuditEvent<T> : EntityAuditEvent
    {
        public EntityUpdatedAuditEvent(string username, string clientId, string subject, T entity)
            : base(username, clientId, subject, FabricIdentityConstants.AuditEventCategory, "Entity Updated", FabricIdentityConstants.CustomEventIds.EntityUpdatedAudit)
        {
            Entity = ObfuscateEntity(entity);
        }

        public T Entity { get; set; }
    }
}
