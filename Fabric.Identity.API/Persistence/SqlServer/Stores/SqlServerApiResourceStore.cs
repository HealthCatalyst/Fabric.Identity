using System;
using System.Linq;
using System.Threading.Tasks;
using Fabric.Identity.API.Persistence.SqlServer.EntityModels;
using Fabric.Identity.API.Persistence.SqlServer.Services;
using Microsoft.EntityFrameworkCore;
using Fabric.Identity.API.Persistence.SqlServer.Mappers;
using ApiResource = IdentityServer4.Models.ApiResource;

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
            var resourceEntity = ApiResourceMapper.ToEntity(resource);

            IdentityDbContext.ApiResources.Add(resourceEntity);
            IdentityDbContext.SaveChanges();
        }

        public void UpdateResource(string id, ApiResource resource)
        {
            var savedResource = IdentityDbContext.ApiResources
                .SingleOrDefault(r => r.Name.Equals(id, StringComparison.OrdinalIgnoreCase)
                            && !r.IsDeleted);

            var resourceEntity = resource.ToEntity();
            resourceEntity.CreatedDateTimeUtc = savedResource.CreatedDateTimeUtc;
            resourceEntity.CreatedBy = savedResource.CreatedBy;
            
            IdentityDbContext.SaveChanges();
        }

        public ApiResource GetResource(string id)
        {
            var apiResource = IdentityDbContext.ApiResources
                .Where(r => r.Name.Equals(id, StringComparison.OrdinalIgnoreCase)
                            && !r.IsDeleted)
                .Include(x => x.ApiSecrets)
                .Include(x => x.ApiScopes)
                .ThenInclude(s => s.ApiScopeClaims)
                .Include(x => x.ApiClaims)
                .FirstOrDefault();

            return apiResource?.ToModel();
        }

        public void DeleteResource(string id)
        {
            var apiResourceToDelete =
                IdentityDbContext.ApiResources.FirstOrDefault(a =>
                    a.Name.Equals(id, StringComparison.OrdinalIgnoreCase));

            apiResourceToDelete.IsDeleted = true;

            IdentityDbContext.SaveChanges();
        }

        public async Task AddResourceAsync(ApiResource resource)
        {
            var resourceEntity = ApiResourceMapper.ToEntity(resource);
            
            IdentityDbContext.ApiResources.Add(resourceEntity);
            await IdentityDbContext.SaveChangesAsync();
        }

        public async Task UpdateResourceAsync(string id, ApiResource resource)
        {
            
            var savedResource = await IdentityDbContext.ApiResources
                .Where(r => r.Name.Equals(id, StringComparison.OrdinalIgnoreCase)
                            && !r.IsDeleted)
                           .SingleOrDefaultAsync();

            //savedResource.ApiClaims = resource.UserClaims.Select(x => new ApiClaim {Type = x}).ToList();
            





            IdentityDbContext.ApiResources.Update(savedResource);
            await IdentityDbContext.SaveChangesAsync();
        }

        public async Task<ApiResource> GetResourceAsync(string id)
        {
            var apiResource = await IdentityDbContext.ApiResources
                .Where(r => r.Name.Equals(id, StringComparison.OrdinalIgnoreCase)
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
                await IdentityDbContext.ApiResources.FirstOrDefaultAsync(a =>
                    a.Name.Equals(id, StringComparison.OrdinalIgnoreCase));

            apiResourceToDelete.IsDeleted = true;

            await IdentityDbContext.SaveChangesAsync();
        }
    }
}
