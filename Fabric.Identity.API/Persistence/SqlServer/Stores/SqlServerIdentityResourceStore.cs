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
using IdentityServer4.Configuration;
using IdentityServer4.Stores;
using Microsoft.Extensions.Logging;

namespace Fabric.Identity.API.Persistence.SqlServer.Stores
{
    public class SqlServerIdentityResourceStore : SqlServerResourceStore, IIdentityResourceStore
    {
        public SqlServerIdentityResourceStore(IIdentityDbContext identityDbContext,
            IEventService eventService,
            IUserResolverService userResolverService,
            ISerializationSettings serializationSettings,
            IdentityServerOptions options,
            IClientStore inner,
            ICache<EntityModels.Client> cache,
            ILogger<SqlServerIdentityResourceStore> logger) : base(identityDbContext, eventService, userResolverService, serializationSettings, options,
            inner,
            cache,
            logger)
        {
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
            await EventService.RaiseAsync(
                new EntityCreatedAuditEvent<IdentityResource>(
                    UserResolverService.Username,
                    UserResolverService.ClientId,
                    UserResolverService.Subject,
                    resource.Name,
                    resource,
                    SerializationSettings));
        }

        public async Task UpdateResourceAsync(string id, IdentityResource resource)
        {
            var existingResource = await IdentityDbContext.IdentityResources
                .Where(r => r.Name == id
                            && !r.IsDeleted)
                .SingleOrDefaultAsync();

           resource.ToEntity(existingResource);

            IdentityDbContext.IdentityResources.Update(existingResource);
            await IdentityDbContext.SaveChangesAsync();
            await EventService.RaiseAsync(
                new EntityUpdatedAuditEvent<IdentityResource>(
                    UserResolverService.Username,
                    UserResolverService.ClientId,
                    UserResolverService.Subject,
                    resource.Name,
                    resource,
                    SerializationSettings));
        }

        public async Task<IdentityResource> GetResourceAsync(string id)
        {
            var identityResourceEntity = await IdentityDbContext.IdentityResources
                .Where(i => !i.IsDeleted)
                .FirstOrDefaultAsync(i => i.Name == id);

            return identityResourceEntity?.ToModel();
        }

        public async Task DeleteResourceAsync(string id)
        {
            var identityResourceToDelete =
                await IdentityDbContext.IdentityResources.FirstOrDefaultAsync(a =>
                    a.Name == id);

            identityResourceToDelete.IsDeleted = true;

            await IdentityDbContext.SaveChangesAsync();
            await EventService.RaiseAsync(
                new EntityDeletedAuditEvent<IdentityResource>(
                    UserResolverService.Username,
                    UserResolverService.ClientId,
                    UserResolverService.Subject,
                    identityResourceToDelete.Name,
                    identityResourceToDelete.ToModel(),
                    SerializationSettings));
        }
    }
}
