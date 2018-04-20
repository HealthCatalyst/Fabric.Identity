using System.Collections.Generic;
using IdentityServer4.Events;
using IdentityServer4.Models;
using Newtonsoft.Json;

namespace Fabric.Identity.API.Events
{
    public abstract class EntityAuditEvent<T> : Event
    {
        private readonly ISerializationSettings _serializationSettings;

        protected EntityAuditEvent(string userName, string clientId, string subject, string documentId, string category,
            string name, int id, ISerializationSettings serializationSettings)
            : base(category, name, EventTypes.Information, id)
        {
            _serializationSettings = serializationSettings;
            Username = userName;
            ClientId = clientId;
            Subject = subject;
            DocumentId = documentId;
            EntityType = typeof(T).FullName;
        }

        protected EntityAuditEvent(string username, string clientId, string subject, string documentId, string category,
            string name,
            int id, T entity, ISerializationSettings serializationSettings)
            : this(username, clientId, subject, documentId, category, name, id, serializationSettings)
        {
            Entity = ObfuscateEntity(entity);
        }

        public string Username { get; set; }
        public string ClientId { get; set; }
        public string Subject { get; set; }
        public string DocumentId { get; set; }
        public string EntityType { get; set; }
        public T Entity { get; set; }

        protected T ObfuscateEntity(T entity)
        {
            var clonedEntity = DeepClone(entity);
            if (typeof(T) == typeof(ApiResource))
            {
                var apiResource = clonedEntity as ApiResource;
                ObfuscateSecrets(apiResource?.ApiSecrets);
                return clonedEntity;
            }

            if (typeof(T) == typeof(Client))
            {
                var client = clonedEntity as Client;
                ObfuscateSecrets(client?.ClientSecrets);
                return clonedEntity;
            }

            return clonedEntity;
        }

        private static void ObfuscateSecrets(IEnumerable<Secret> secrets)
        {
            foreach (var secret in secrets)
            {
                secret.Value = Obfuscate(secret.Value);
            }
        }

        private T DeepClone(T entity)
        {
            var serializedEntity = JsonConvert.SerializeObject(entity, _serializationSettings.JsonSettings);
            return JsonConvert.DeserializeObject<T>(serializedEntity, _serializationSettings.JsonSettings);
        }
    }
}