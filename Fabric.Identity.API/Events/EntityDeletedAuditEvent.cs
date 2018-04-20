namespace Fabric.Identity.API.Events
{
    public class EntityDeletedAuditEvent<T> : EntityAuditEvent<T>
    {
        public EntityDeletedAuditEvent(string userName, string clientId, string subject, string documentId, T entity,
            ISerializationSettings serializationSettings)
            : base(userName,
                clientId,
                subject,
                documentId,
                FabricIdentityConstants.AuditEventCategory,
                FabricIdentityConstants.CustomEventNames.EntityDeletedAudit,
                FabricIdentityConstants.CustomEventIds.EntityDeletedAudit,
                entity,
                serializationSettings)
        {
        }

        public EntityDeletedAuditEvent(string username, string clientId, string subject, string documentId,
            ISerializationSettings serializationSettings)
            : base(username,
                clientId,
                subject,
                documentId,
                FabricIdentityConstants.AuditEventCategory,
                FabricIdentityConstants.CustomEventNames.EntityDeletedAudit,
                FabricIdentityConstants.CustomEventIds.EntityDeletedAudit,
                serializationSettings)
        {
        }
    }
}