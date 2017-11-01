using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using IdentityServer4.Models;

namespace Fabric.Identity.API.Persistence.CouchDb.Stores
{
    public class CouchDbPersistedGrantStore : IPersistedGrantStore
    {
        private readonly IDocumentDbService _documentDbService;

        public CouchDbPersistedGrantStore(IDocumentDbService documentDbService)
        {
            _documentDbService = documentDbService;
        }

        public Task StoreAsync(PersistedGrant grant)
        {
            _documentDbService.AddDocument(grant.Key, grant);

            return Task.FromResult(0);
        }

        public Task<PersistedGrant> GetAsync(string key)
        {
            return _documentDbService.GetDocument<PersistedGrant>(key);
        }

        public Task<IEnumerable<PersistedGrant>> GetAllAsync(string subjectId)
        {
            var persistedGrants = _documentDbService
                .GetDocuments<PersistedGrant>(FabricIdentityConstants.DocumentTypes.PersistedGrantDocumentType).Result;

            var matchingGrants =
                persistedGrants.Where(p => p.SubjectId.Equals(subjectId, StringComparison.OrdinalIgnoreCase));

            return Task.FromResult(matchingGrants);
        }

        public Task RemoveAsync(string key)
        {
            _documentDbService.DeleteDocument<PersistedGrant>(key);
            return Task.FromResult(0);
        }

        public Task RemoveAllAsync(string subjectId, string clientId)
        {
            var persistedGrants = _documentDbService
                .GetDocuments<PersistedGrant>(FabricIdentityConstants.DocumentTypes.PersistedGrantDocumentType).Result;
            var matchingGrants = persistedGrants.Where(
                g => g.SubjectId.Equals(subjectId, StringComparison.OrdinalIgnoreCase)
                     && g.ClientId.Equals(clientId, StringComparison.OrdinalIgnoreCase));

            foreach (var persistedGrant in matchingGrants)
            {
                _documentDbService.DeleteDocument<PersistedGrant>(persistedGrant.Key);
            }

            return Task.FromResult(0);
        }

        public Task RemoveAllAsync(string subjectId, string clientId, string type)
        {
            var persistedGrants = _documentDbService
                .GetDocuments<PersistedGrant>(FabricIdentityConstants.DocumentTypes.PersistedGrantDocumentType).Result;
            var matchingGrants = persistedGrants.Where(
                g => g.SubjectId.Equals(subjectId, StringComparison.OrdinalIgnoreCase)
                     && g.ClientId.Equals(clientId, StringComparison.OrdinalIgnoreCase)
                     && g.Type.Equals(type, StringComparison.OrdinalIgnoreCase));

            foreach (var persistedGrant in matchingGrants)
            {
                _documentDbService.DeleteDocument<PersistedGrant>(persistedGrant.Key);
            }

            return Task.FromResult(0);
        }
    }
}