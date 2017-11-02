using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fabric.Identity.API.Persistence.SqlServer.Models;
using Fabric.Identity.API.Persistence.SqlServer.Services;
using IdentityServer4.EntityFramework.Mappers;
using IdentityServer4.Models;
using IdentityServer4.Stores;
using Microsoft.EntityFrameworkCore;

namespace Fabric.Identity.API.Persistence.SqlServer.Stores
{
    public class SqlServerApiResourceStore : IApiResourceStore, IResourceStore
    {
        private readonly IIdentityDbContext _identityDbContext;

        public SqlServerApiResourceStore(IIdentityDbContext identityDbContext)
        {
            _identityDbContext = identityDbContext;
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
            var resourceDomainModel = resource.ToDomainModel();

            //TODO: set domain model properties

            await _identityDbContext.ApiResources.AddAsync(resourceDomainModel);
        }

        public async Task UpdateResourceAsync(string id, ApiResource resource)
        {
            var apiResourceDomainModelDomainModel = resource.ToDomainModel();

            //TODO: set domain model properties

            _identityDbContext.ApiResources.Update(apiResourceDomainModelDomainModel);
            await _identityDbContext.SaveChangesAsync();
        }

        public async Task<ApiResource> GetResourceAsync(string id)
        {
            return await FindApiResourceAsync(id);
        }

        public async Task DeleteResourceAsync(string id)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<IdentityResource>> FindIdentityResourcesByScopeAsync(IEnumerable<string> scopeNames)
        {
            throw new NotImplementedException();
        }

        public async Task<IEnumerable<ApiResource>> FindApiResourcesByScopeAsync(IEnumerable<string> scopeNames)
        {
            var scopes = scopeNames.ToArray();

            var apiResources = await _identityDbContext.ApiResources
                .Where(r => scopes.Contains(r.Name))
                .Include(x => x.UserClaims)
                .ToArrayAsync();

            return apiResources.Select(r => r.ToModel());
        }

        public async Task<ApiResource> FindApiResourceAsync(string name)
        {
            var apiResource = await _identityDbContext.ApiResources
                .Where(r => r.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
                .Include(x => x.Secrets)
                .Include(x => x.Scopes)
                    .ThenInclude(s => s.UserClaims)
                .Include(x => x.UserClaims)
                .FirstOrDefaultAsync();

            return apiResource.ToModel();
        }

        public async Task<Resources> GetAllResources()
        {
            var identity = _identityDbContext.IdentityResources
                .Include(x => x.UserClaims);

            var apis = _identityDbContext.ApiResources
                .Include(x => x.Secrets)
                .Include(x => x.Scopes)
                .ThenInclude(s => s.UserClaims)
                .Include(x => x.UserClaims);

            var identityResources = await identity.ToArrayAsync();
            var apiResources = await apis.ToArrayAsync();

            var result = new Resources(
                identityResources.Select(x => x.ToModel()),
                apiResources.Select(x => x.ToModel()));

            return result;
        }
    }
}
