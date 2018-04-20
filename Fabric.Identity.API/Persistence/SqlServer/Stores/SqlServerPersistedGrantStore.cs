using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using Fabric.Identity.API.Events;
using Fabric.Identity.API.Persistence.SqlServer.Services;
using Microsoft.EntityFrameworkCore;
using Fabric.Identity.API.Persistence.SqlServer.EntityModels;
using Fabric.Identity.API.Persistence.SqlServer.Mappers;
using Fabric.Identity.API.Services;
using IdentityServer4.Events;
using IdentityServer4.Services;
using Serilog;

namespace Fabric.Identity.API.Persistence.SqlServer.Stores
{
    public class SqlServerPersistedGrantStore : SqlServerBaseStore, IPersistedGrantStore
    {
        private readonly ILogger _logger;

        public SqlServerPersistedGrantStore(IIdentityDbContext identityDbContext,
            ILogger logger,
            IEventService eventService,
            IUserResolverService userResolverService,
            ISerializationSettings serializationSettings) : base(identityDbContext, eventService, userResolverService, serializationSettings)
        {
            _logger = logger;
        }

        public Task<IEnumerable<IdentityServer4.Models.PersistedGrant>> GetAllAsync(string subjectId)
        {
            var persistedGrants = IdentityDbContext.PersistedGrants
                .Where(pg => pg.SubjectId == subjectId
                             && !pg.IsDeleted);
            return Task.FromResult(persistedGrants.Select(pg => pg.ToModel()).AsEnumerable());
        }

        public async Task<IdentityServer4.Models.PersistedGrant> GetAsync(string key)
        {
            var persistedGrantEntity = await IdentityDbContext.PersistedGrants
                .Where(pg => !pg.IsDeleted)
                .FirstOrDefaultAsync(pg => pg.Key == key);
            return persistedGrantEntity?.ToModel();
        }

        public async Task RemoveAllAsync(string subjectId, string clientId)
        {
            var persistedGrantEntities = IdentityDbContext.PersistedGrants
                .Where(pg => pg.SubjectId == subjectId
                             && pg.ClientId == clientId);

            await DeletePersistedGrantsAsync(persistedGrantEntities);
        }

        public async Task RemoveAllAsync(string subjectId, string clientId, string type)
        {
            var persistedGrantEntities = IdentityDbContext.PersistedGrants
                .Where(pg => pg.SubjectId == subjectId
                             && pg.ClientId == clientId
                             && pg.Type == type);

            await DeletePersistedGrantsAsync(persistedGrantEntities);
        }

        public async Task RemoveAsync(string key)
        {
            var persistedGrantEntities = IdentityDbContext.PersistedGrants
                .Where(pg => pg.Key == key);

            await DeletePersistedGrantsAsync(persistedGrantEntities);
        }

        private async Task DeletePersistedGrantsAsync(IQueryable<PersistedGrant> persistedGrants)
        {
            await persistedGrants.ForEachAsync(pg => IdentityDbContext.PersistedGrants.Remove(pg));
            try
            {
                await IdentityDbContext.SaveChangesAsync();
                foreach (var persistedGrant in persistedGrants)
                {
                    await EventService.RaiseAsync(
                        new EntityDeletedAuditEvent<PersistedGrant>(
                            UserResolverService.Username,
                            UserResolverService.ClientId,
                            UserResolverService.Subject,
                            persistedGrant.Key,
                            persistedGrant,
                            SerializationSettings));
                }
            }
            catch (DbUpdateConcurrencyException ex)
            {
                _logger.Warning("Exception removing persistedGrants from the database. Error: {message}", ex.Message);
            }
        }

        public async Task StoreAsync(IdentityServer4.Models.PersistedGrant grant)
        {
            var existingGrant = IdentityDbContext.PersistedGrants.SingleOrDefault(pg => pg.Key == grant.Key);
            Event evt;
            if (existingGrant == null)
            {
                var persistedGrantEntity = grant.ToEntity();
                IdentityDbContext.PersistedGrants.Add(persistedGrantEntity);
                evt = new EntityCreatedAuditEvent<PersistedGrant>(
                    UserResolverService.Username,
                    UserResolverService.ClientId,
                    UserResolverService.Subject,
                    persistedGrantEntity.Key,
                    persistedGrantEntity,
                    SerializationSettings);
            }
            else
            {
                grant.ToEntity(existingGrant);
                evt = new EntityUpdatedAuditEvent<PersistedGrant>(
                    UserResolverService.Username,
                    UserResolverService.ClientId,
                    UserResolverService.Subject,
                    existingGrant.Key,
                    existingGrant,
                    SerializationSettings);
            }
            try
            {
                await IdentityDbContext.SaveChangesAsync();
                await EventService.RaiseAsync(evt);
            }
            catch (DbUpdateConcurrencyException ex)
            {
                _logger.Warning("Exception updating {grantKey}. Error: {error}", grant.Key, ex.Message);
            }
        }
    }
}