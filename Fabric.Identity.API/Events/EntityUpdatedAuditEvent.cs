namespace Fabric.Identity.API.Events
{
    public class EntityUpdatedAuditEvent<T> : EntityAuditEvent<T>
    {
        public EntityUpdatedAuditEvent(string userName, string clientId, string subject, string documentId, T entity, ISerializationSettings serializationSettings)
            : base(userName, 
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
