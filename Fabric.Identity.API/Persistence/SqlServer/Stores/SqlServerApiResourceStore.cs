using System;
using System.Linq;
using System.Threading.Tasks;
using Fabric.Identity.API.Events;
using Fabric.Identity.API.Persistence.SqlServer.Services;
using Microsoft.EntityFrameworkCore;
using Fabric.Identity.API.Persistence.SqlServer.Mappers;
using Fabric.Identity.API.Services;
using IdentityServer4.Services;
using ApiResource = IdentityServer4.Models.ApiResource;

namespace Fabric.Identity.API.Persistence.SqlServer.Stores
{
    public class SqlServerApiResourceStore : SqlServerResourceStore, IApiResourceStore
    {
        public SqlServerApiResourceStore(IIdentityDbContext identityDbContext,
            IEventService eventService,
            IUserResolverService userResolverService,
            ISerializationSettings serializationSettings)
            : base(identityDbContext, eventService, userResolverService, serializationSettings)
        {
        }

        public void AddResource(ApiResource resource)
        {
            AddResourceAsync(resource).Wait();
        }

        public void UpdateResource(string id, ApiResource resource)
        {
            UpdateResourceAsync(id, resource).Wait();
        }

        public ApiResource GetResource(string id)
        {
            return GetResourceAsync(id).Result;
        }

        public void DeleteResource(string id)
        {
            DeleteResourceAsync(id).Wait();
        }

        public async Task AddResourceAsync(ApiResource resource)
        {
            var resourceEntity = resource.ToEntity();

            IdentityDbContext.ApiResources.Add(resourceEntity);
            await IdentityDbContext.SaveChangesAsync();
            await EventService.RaiseAsync(
                new EntityCreatedAuditEvent<ApiResource>(
                    UserResolverService.Username,
                    UserResolverService.ClientId,
                    UserResolverService.Subject,
                    resource.Name,
                    resource,
                    SerializationSettings));
        }

        public async Task UpdateResourceAsync(string id, ApiResource resource)
        {
            var savedResource = await IdentityDbContext.ApiResources
                .Where(r => r.Name == id
                            && !r.IsDeleted)
                           .SingleOrDefaultAsync();

           resource.ToEntity(savedResource);

            IdentityDbContext.ApiResources.Update(savedResource);
            await IdentityDbContext.SaveChangesAsync();
            await EventService.RaiseAsync(
                new EntityUpdatedAuditEvent<ApiResource>(
                    UserResolverService.Username,
                    UserResolverService.ClientId,
                    UserResolverService.Subject,
                    resource.Name,
                    resource,
                    SerializationSettings));
        }

        public async Task<ApiResource> GetResourceAsync(string id)
        {
            var apiResource = await IdentityDbContext.ApiResources
                .Where(r => r.Name == id
                            && !r.IsDeleted)
                .Include(x => x.ApiSecrets)
                .Include(x => x.ApiScopes)
                .ThenInclude(s => s.ApiScopeClaims)
                .Include(x => x.ApiClaims)
                .FirstOrDefaultAsync();

            return apiResource?.ToModel();
        }

        public async Task DeleteResourceAsync(string id)
        {
            var apiResourceToDelete =
                await IdentityDbContext.ApiResources
                    .Where(r => r.Name == id)
                    .Include(r => r.ApiScopes)
                    .FirstOrDefaultAsync();

            apiResourceToDelete.IsDeleted = true;
            foreach (var apiScope in apiResourceToDelete.ApiScopes)
            {
                apiScope.IsDeleted = true;
            }

            await IdentityDbContext.SaveChangesAsync();
            await EventService.RaiseAsync(
                new EntityDeletedAuditEvent<ApiResource>(
                    UserResolverService.Username,
                    UserResolverService.ClientId,
                    UserResolverService.Subject,
                    apiResourceToDelete.Name,
                    apiResourceToDelete.ToModel(),
                    SerializationSettings));
        }
    }
}
