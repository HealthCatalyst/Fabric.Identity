using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using IdentityServer4.Models;
using IdentityServer4.Stores;

namespace Fabric.Identity.API.CouchDb
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
            _documentDbService.AddOrUpdateDocument(grant.Key, grant);

            return Task.FromResult(0);
        }

        public Task<PersistedGrant> GetAsync(string key)
        {
            return _documentDbService.FindDocumentByKey<PersistedGrant>("persistedgrant", new []{key});
        }

        public Task<IEnumerable<PersistedGrant>> GetAllAsync(string subjectId)
        {
            return _documentDbService.FindDocumentsByKey<PersistedGrant>("persistedgrantsubject", new []{subjectId});
        }

        public Task RemoveAsync(string key)
        {
            _documentDbService.DeleteDocument("persistedgrant", new[] { key });
            return Task.FromResult(0);
        }

        public Task RemoveAllAsync(string subjectId, string clientId)
        {
            _documentDbService.DeleteDocument("persistedgrantsubjectclient", new[] {subjectId, clientId});
            return Task.FromResult(0);
        }

        public Task RemoveAllAsync(string subjectId, string clientId, string type)
        {
            _documentDbService.DeleteDocument("persistedgrantsubjectclienttype", new[] { subjectId, clientId, type });
            return Task.FromResult(0);
        }
    }
}
