namespace Fabric.Identity.API.Events
{
    public class EntityReadAuditEvent<T> : EntityAuditEvent<T>
    {
        public EntityReadAuditEvent(string userName, string clientId, string subject, string documentId,
            ISerializationSettings serializationSettings)
            : base(userName,
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