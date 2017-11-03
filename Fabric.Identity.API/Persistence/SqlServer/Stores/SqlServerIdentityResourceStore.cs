using System;
using System.Threading.Tasks;
using Fabric.Identity.API.Persistence.SqlServer.Models;
using Fabric.Identity.API.Persistence.SqlServer.Services;
using IdentityServer4.EntityFramework.Mappers;
using IdentityServer4.Models;
using Microsoft.EntityFrameworkCore;

namespace Fabric.Identity.API.Persistence.SqlServer.Stores
{
    public class SqlServerIdentityResourceStore : SqlServerResourceStore, IIdentityResourceStore
    {
        public SqlServerIdentityResourceStore(IIdentityDbContext identityDbContext) :
            base(identityDbContext)
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
            var resourceEntity = resource.ToFabricEntity();

            //TODO: set entity properties

            await IdentityDbContext.IdentityResources.AddAsync(resourceEntity);
        }

        public async Task UpdateResourceAsync(string id, IdentityResource resource)
        {
            var identityResourceEntity = resource.ToFabricEntity();

            //TODO: set entity properties

            IdentityDbContext.IdentityResources.Update(identityResourceEntity);
            await IdentityDbContext.SaveChangesAsync();
        }

        public async Task<IdentityResource> GetResourceAsync(string id)
        {
            var identityResourceEntity = await IdentityDbContext.IdentityResources
                .FirstOrDefaultAsync(i => i.Name.Equals(id, StringComparison.CurrentCultureIgnoreCase));

            return identityResourceEntity?.ToModel();
        }

        public async Task DeleteResourceAsync(string id)
        {
            var identityResourceToDelete =
                await IdentityDbContext.IdentityResources.FirstOrDefaultAsync(a =>
                    a.Name.Equals(id, StringComparison.OrdinalIgnoreCase));

            //TODO: set other entity properties

            identityResourceToDelete.IsDeleted = true;

            await IdentityDbContext.SaveChangesAsync();
        }
    }
}
