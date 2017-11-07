using System.Threading.Tasks;
using IdentityServer4.Models;
using System.Collections.Generic;
using System.Linq;
using Fabric.Identity.API.Persistence.SqlServer.Entities;
using Fabric.Identity.API.Persistence.SqlServer.Services;
using Microsoft.EntityFrameworkCore;


namespace Fabric.Identity.API.Persistence.SqlServer.Stores
{
    public class SqlServerPersistedGrantStore : IPersistedGrantStore
    {
        private readonly IIdentityDbContext _identityDbContext;

        public SqlServerPersistedGrantStore(IIdentityDbContext identityDbContext)
        {
            _identityDbContext = identityDbContext;
        }

        public Task<IEnumerable<PersistedGrant>> GetAllAsync(string subjectId)
        {
            var persistedGrants = _identityDbContext.PersistedGrants.Where(pg => pg.SubjectId == subjectId);
            return Task.FromResult(persistedGrants.Select(pg => pg.ToModel()).AsEnumerable());
        }

        public async Task<PersistedGrant> GetAsync(string key)
        {
            var persistedGrantEntity = await _identityDbContext.PersistedGrants.FirstOrDefaultAsync(pg => pg.Key == key);
            return persistedGrantEntity?.ToModel();
        }

        public async Task RemoveAllAsync(string subjectId, string clientId)
        {
            var persistedGrantEntities = _identityDbContext.PersistedGrants
                .Where(pg => pg.SubjectId == subjectId
                             && pg.ClientId == clientId);

            await DeletePersistedGrantsAsync(persistedGrantEntities);
        }

        public async Task RemoveAllAsync(string subjectId, string clientId, string type)
        {
            var persistedGrantEntities = _identityDbContext.PersistedGrants
                .Where(pg => pg.SubjectId == subjectId
                             && pg.ClientId == clientId
                             && pg.Type == type);

            await DeletePersistedGrantsAsync(persistedGrantEntities);
        }

        public async Task RemoveAsync(string key)
        {
            var persistedGrantEntities = _identityDbContext.PersistedGrants
                .Where(pg => pg.Key == key);

            await DeletePersistedGrantsAsync(persistedGrantEntities);
        }

        private async Task DeletePersistedGrantsAsync(IQueryable<EntityModels.PersistedGrant> persistedGrants)
        {
            await persistedGrants.ForEachAsync(pg => pg.IsDeleted = true);
            await _identityDbContext.SaveChangesAsync();
        }

        public Task StoreAsync(PersistedGrant grant)
        {
            var persistedGrantEntity = grant.ToFabricEntity();
            return _identityDbContext.PersistedGrants.AddAsync(persistedGrantEntity);
        }
    }
}