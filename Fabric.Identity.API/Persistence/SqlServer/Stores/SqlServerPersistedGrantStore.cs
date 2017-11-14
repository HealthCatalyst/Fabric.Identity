using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using Fabric.Identity.API.Persistence.SqlServer.Services;
using Microsoft.EntityFrameworkCore;
using Fabric.Identity.API.Persistence.SqlServer.EntityModels;
using Fabric.Identity.API.Persistence.SqlServer.Mappers;


namespace Fabric.Identity.API.Persistence.SqlServer.Stores
{
    public class SqlServerPersistedGrantStore : IPersistedGrantStore
    {
        private readonly IIdentityDbContext _identityDbContext;

        public SqlServerPersistedGrantStore(IIdentityDbContext identityDbContext)
        {
            _identityDbContext = identityDbContext;
        }

        public Task<IEnumerable<IdentityServer4.Models.PersistedGrant>> GetAllAsync(string subjectId)
        {
            var persistedGrants = _identityDbContext.PersistedGrants
                .Where(pg => pg.SubjectId == subjectId
                             && !pg.IsDeleted);
            return Task.FromResult(persistedGrants.Select(pg => pg.ToModel()).AsEnumerable());
        }

        public async Task<IdentityServer4.Models.PersistedGrant> GetAsync(string key)
        {
            var persistedGrantEntity = await _identityDbContext.PersistedGrants
                .Where(pg => !pg.IsDeleted)
                .FirstOrDefaultAsync(pg => pg.Key == key);
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

        private async Task DeletePersistedGrantsAsync(IQueryable<PersistedGrant> persistedGrants)
        {
            await persistedGrants.ForEachAsync(pg => pg.IsDeleted = true);
            await _identityDbContext.SaveChangesAsync();
        }

        public Task StoreAsync(IdentityServer4.Models.PersistedGrant grant)
        {
            var persistedGrantEntity = grant.ToEntity();
            return _identityDbContext.PersistedGrants.AddAsync(persistedGrantEntity);
        }
    }
}