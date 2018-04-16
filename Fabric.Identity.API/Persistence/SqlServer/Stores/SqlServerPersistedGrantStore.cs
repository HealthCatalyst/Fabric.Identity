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
    public class SqlServerPersistedGrantStore : IPersistedGrantStore
    {
        private readonly IIdentityDbContext _identityDbContext;
        private readonly ILogger _logger;
        private readonly IUserResolverService _userResolverService;
        private readonly IEventService _eventService;
        private readonly ISerializationSettings _serializationSettings;

        public SqlServerPersistedGrantStore(IIdentityDbContext identityDbContext,
            ILogger logger,
            IEventService eventService,
            IUserResolverService userResolverService,
            ISerializationSettings serializationSettings)
        {
            _identityDbContext = identityDbContext;
            _logger = logger;
            _eventService = eventService;
            _userResolverService = userResolverService;
            _serializationSettings = serializationSettings;
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
            await persistedGrants.ForEachAsync(pg => _identityDbContext.PersistedGrants.Remove(pg));
            try
            {
                await _identityDbContext.SaveChangesAsync();
                foreach (var persistedGrant in persistedGrants)
                {
                    await _eventService.RaiseAsync(
                        new EntityDeletedAuditEvent<PersistedGrant>(
                            _userResolverService.Username,
                            _userResolverService.ClientId,
                            _userResolverService.Subject,
                            persistedGrant.Key,
                            persistedGrant,
                            _serializationSettings));
                }
            }
            catch (DbUpdateConcurrencyException ex)
            {
                _logger.Warning("Exception removing persistedGrants from the database. Error: {message}", ex.Message);
            }
        }

        public async Task StoreAsync(IdentityServer4.Models.PersistedGrant grant)
        {
            var existingGrant = _identityDbContext.PersistedGrants.SingleOrDefault(pg => pg.Key == grant.Key);
            Event evt;
            if (existingGrant == null)
            {
                var persistedGrantEntity = grant.ToEntity();
                _identityDbContext.PersistedGrants.Add(persistedGrantEntity);
                evt = new EntityCreatedAuditEvent<PersistedGrant>(
                    _userResolverService.Username,
                    _userResolverService.ClientId,
                    _userResolverService.Subject,
                    persistedGrantEntity.Key,
                    persistedGrantEntity,
                    _serializationSettings);
            }
            else
            {
                grant.ToEntity(existingGrant);
                evt = new EntityUpdatedAuditEvent<PersistedGrant>(
                    _userResolverService.Username,
                    _userResolverService.ClientId,
                    _userResolverService.Subject,
                    existingGrant.Key,
                    existingGrant,
                    _serializationSettings);
            }
            try
            {
                await _identityDbContext.SaveChangesAsync();
                await _eventService.RaiseAsync(evt);
            }
            catch (DbUpdateConcurrencyException ex)
            {
                _logger.Warning("Exception updating {grantKey}. Error: {error}", grant.Key, ex.Message);
            }
        }
    }
}