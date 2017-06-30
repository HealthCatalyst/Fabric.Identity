using System.Collections.Generic;
using IdentityServer4.Events;
using IdentityServer4.Models;
using Newtonsoft.Json;

namespace Fabric.Identity.API.Events
{
    public abstract class EntityAuditEvent<T> : Event
    {
        protected EntityAuditEvent(string username, string clientId, string subject, string documentId, string category, string name, int id) 
            : base(category, name, EventTypes.Information, id)
        {
            Username = username;
            ClientId = clientId;
            Subject = subject;
            DocumentId = documentId;
            EntityType = typeof(T).FullName;
        }

        protected EntityAuditEvent(string username, string clientId, string subject, string documentId, string category, string name,
            int id, T entity)
            : this(username, clientId, subject, documentId, category, name, id)
        {
            Entity = ObfuscateEntity(entity);
        }

        public string Username { get; set; }
        public string ClientId { get; set; }
        public string Subject { get; set; }
        public string DocumentId { get; set; }
        public string EntityType { get; set; }
        public T Entity { get; set; }

        protected static T ObfuscateEntity<T>(T entity)
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

        private static void ObfuscateSecrets(ICollection<Secret> secrets)
        {
            foreach (var secret in secrets)
            {
                secret.Value = Obfuscate(secret.Value);
            }
        }

        private static T DeepClone<T>(T entity)
        {
            var serializedEntity = JsonConvert.SerializeObject(entity);
            return JsonConvert.DeserializeObject<T>(serializedEntity);
        }
    }
}
