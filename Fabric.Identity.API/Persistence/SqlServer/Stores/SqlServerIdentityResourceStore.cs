using System;
using System.Linq;
using System.Threading.Tasks;
using Fabric.Identity.API.Events;
using Fabric.Identity.API.Persistence.SqlServer.Services;
using Microsoft.EntityFrameworkCore;
using Fabric.Identity.API.Persistence.SqlServer.Mappers;
using Fabric.Identity.API.Services;
using IdentityServer4.Services;
using IdentityResource = IdentityServer4.Models.IdentityResource;

namespace Fabric.Identity.API.Persistence.SqlServer.Stores
{
    public class SqlServerIdentityResourceStore : SqlServerResourceStore, IIdentityResourceStore
    {
        private readonly IUserResolverService _userResolverService;
        private readonly IEventService _eventService;
        private readonly ISerializationSettings _serializationSettings;

        public SqlServerIdentityResourceStore(IIdentityDbContext identityDbContext,
            IEventService eventService,
            IUserResolverService userResolverService,
            ISerializationSettings serializationSettings) :
            base(identityDbContext)
        {
            _eventService = eventService;
            _userResolverService = userResolverService;
            _serializationSettings = serializationSettings;
        }

        public void AddResource(IdentityResource resource)
        {
            AddResourceAsync(resource).Wait();
        }

        public void UpdateResource(string id, IdentityResource resource)
        {
            UpdateResourceAsync(id, resource).Wait();
        }

        public IdentityResource GetResource(string id)
        {
            return GetResourceAsync(id).Result;
        }

        public void DeleteResource(string id)
        {
            DeleteResourceAsync(id).Wait();
        }

        public async Task AddResourceAsync(IdentityResource resource)
        {
            var resourceEntity = resource.ToEntity();

            IdentityDbContext.IdentityResources.Add(resourceEntity);
            await IdentityDbContext.SaveChangesAsync();
            await _eventService.RaiseAsync(
                new EntityCreatedAuditEvent<IdentityResource>(
                    _userResolverService.Username,
                    _userResolverService.ClientId,
                    _userResolverService.Subject,
                    resource.Name,
                    resource,
                    _serializationSettings));
        }

        public async Task UpdateResourceAsync(string id, IdentityResource resource)
        {
            var existingResource = await IdentityDbContext.IdentityResources
                .Where(r => r.Name.Equals(id, StringComparison.OrdinalIgnoreCase)
                            && !r.IsDeleted)
                .SingleOrDefaultAsync();

           resource.ToEntity(existingResource);
            
            IdentityDbContext.IdentityResources.Update(existingResource);
            await IdentityDbContext.SaveChangesAsync();
            await _eventService.RaiseAsync(
                new EntityUpdatedAuditEvent<IdentityResource>(
                    _userResolverService.Username,
                    _userResolverService.ClientId,
                    _userResolverService.Subject,
                    resource.Name,
                    resource,
                    _serializationSettings));
        }

        public async Task<IdentityResource> GetResourceAsync(string id)
        {
            var identityResourceEntity = await IdentityDbContext.IdentityResources
                .Where(i => !i.IsDeleted)
                .FirstOrDefaultAsync(i => i.Name.Equals(id, StringComparison.CurrentCultureIgnoreCase));

            return identityResourceEntity?.ToModel();
        }

        public async Task DeleteResourceAsync(string id)
        {
            var identityResourceToDelete =
                await IdentityDbContext.IdentityResources.FirstOrDefaultAsync(a =>
                    a.Name.Equals(id, StringComparison.OrdinalIgnoreCase));

            identityResourceToDelete.IsDeleted = true;

            await IdentityDbContext.SaveChangesAsync();
            await _eventService.RaiseAsync(
                new EntityDeletedAuditEvent<IdentityResource>(
                    _userResolverService.Username,
                    _userResolverService.ClientId,
                    _userResolverService.Subject,
                    identityResourceToDelete.Name,
                    identityResourceToDelete.ToModel(),
                    _serializationSettings));
        }
    }
}
