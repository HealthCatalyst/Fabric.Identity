using System;
using System.Linq;
using System.Threading.Tasks;
using Fabric.Identity.API.Persistence.SqlServer.Entities;
using Fabric.Identity.API.Persistence.SqlServer.Services;
using IdentityServer4.EntityFramework.Mappers;
using IdentityServer4.Models;
using Microsoft.EntityFrameworkCore;

namespace Fabric.Identity.API.Persistence.SqlServer.Stores
{
    public class SqlServerApiResourceStore : SqlServerResourceStore, IApiResourceStore
    {
        public SqlServerApiResourceStore(IIdentityDbContext identityDbContext)
            : base(identityDbContext)
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
            var resourceEntity = resource.ToFabricEntity();

            //TODO: set entity properties

            await IdentityDbContext.ApiResources.AddAsync(resourceEntity);
        }

        public async Task UpdateResourceAsync(string id, ApiResource resource)
        {
            var resourceEntity = resource.ToFabricEntity();

            //TODO: set entity properties

            IdentityDbContext.ApiResources.Update(resourceEntity);
            await IdentityDbContext.SaveChangesAsync();
        }

        public async Task<ApiResource> GetResourceAsync(string id)
        {
            var apiResource = await IdentityDbContext.ApiResources
                .Where(r => r.Name.Equals(id, StringComparison.OrdinalIgnoreCase))
                .Include(x => x.Secrets)
                .Include(x => x.Scopes)
                .ThenInclude(s => s.UserClaims)
                .Include(x => x.UserClaims)
                .FirstOrDefaultAsync();

            return apiResource?.ToModel();
        }

        public async Task DeleteResourceAsync(string id)
        {
            var apiResourceToDelete =
                await IdentityDbContext.ApiResources.FirstOrDefaultAsync(a =>
                    a.Name.Equals(id, StringComparison.OrdinalIgnoreCase));

            //TODO: set other entity properties

            apiResourceToDelete.IsDeleted = true;

            await IdentityDbContext.SaveChangesAsync();
        }
    }
}
