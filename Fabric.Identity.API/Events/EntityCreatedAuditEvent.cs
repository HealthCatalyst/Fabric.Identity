namespace Fabric.Identity.API.Events
{
    public class EntityCreatedAuditEvent<T> : EntityAuditEvent<T>
    {
        public EntityCreatedAuditEvent(string userName, string clientId, string subject, string documentId, T entity,
            ISerializationSettings serializationSettings)
            : base(userName,
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