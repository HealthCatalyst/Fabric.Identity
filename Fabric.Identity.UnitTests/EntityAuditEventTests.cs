using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Fabric.Identity.API;
using Fabric.Identity.API.Events;
using IdentityServer4.Models;
using MyCouch;
using Xunit;

namespace Fabric.Identity.UnitTests
{
    public class EntityAuditEventTests
    {
        [Fact]
        public void Obfuscate_Succeeds()
        {
            var clearTextSecrets = new List<string> {Guid.NewGuid().ToString(), Guid.NewGuid().ToString()};
            var secrets = clearTextSecrets.Select(s => new Secret(s)).ToList();
            var apiResource = new ApiResource
            {
                ApiSecrets = secrets,
                Name = "test-api"
            };

            var createdAuditEvent = new EntityCreatedAuditEvent<ApiResource>("username", "clientid", "subject", apiResource.Name, apiResource);
            
            foreach (var secret in clearTextSecrets)
            {
                //Make sure that the secrets haven't been changed in the original reference
                Assert.True(apiResource.ApiSecrets.Any(s => s.Value == secret));
                //Make sure that the secrets in the object that gets serialized have been obfuscated
                Assert.True(createdAuditEvent.Entity.ApiSecrets.Any(s => s.Value == $"****{secret.Substring(secret.Length-4)}"));
            }
        }

        [Fact]
        public void CreateEvent_Succeeds()
        {
            var expectedUsername = "username";
            var expectedClientId = "clientid";
            var expectedSubject = "subect";
            var expectedDocumentId = "123456";
            var readAuditEvent = new EntityReadAuditEvent<Client>("username", "clientid", "subect", "123456");
            Assert.Equal(typeof(Client).FullName, readAuditEvent.EntityType);
            Assert.Equal(expectedUsername, readAuditEvent.Username);
            Assert.Equal(expectedClientId, readAuditEvent.ClientId);
            Assert.Equal(expectedSubject, readAuditEvent.Subject);
            Assert.Equal(expectedDocumentId, readAuditEvent.DocumentId);
            Assert.Equal(FabricIdentityConstants.CustomEventNames.EntityReadAudit, readAuditEvent.Name);
            Assert.Null(readAuditEvent.Entity);
        }
    }
}
