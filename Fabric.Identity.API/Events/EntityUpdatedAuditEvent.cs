namespace Fabric.Identity.API.Events
{
    public class EntityUpdatedAuditEvent<T> : EntityAuditEvent<T>
    {
        public EntityUpdatedAuditEvent(string username, string clientId, string subject, string documentId, T entity, ISerializationSettings serializationSettings)
            : base(username, 
                  clientId, 
                  subject, 
                  documentId, 
                  FabricIdentityConstants.AuditEventCategory, 
                  FabricIdentityConstants.CustomEventNames.EntityUpdatedAudit, 
                  FabricIdentityConstants.CustomEventIds.EntityUpdatedAudit, 
                  entity,
                  serializationSettings)
        {
        }
    }
}
