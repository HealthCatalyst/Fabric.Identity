using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fabric.Identity.API.Persistence.SqlServer.Services;
using IdentityServer4.EntityFramework.Mappers;
using IdentityServer4.Models;
using IdentityServer4.Stores;
using Microsoft.EntityFrameworkCore;

namespace Fabric.Identity.API.Persistence.SqlServer.Stores
{
    public class SqlServerResourceStore : IResourceStore
    {
        protected readonly IIdentityDbContext IdentityDbContext;

        public SqlServerResourceStore(IIdentityDbContext identityDbContext)
        {
            IdentityDbContext = identityDbContext;
        }

        public async Task<IEnumerable<IdentityResource>> FindIdentityResourcesByScopeAsync(IEnumerable<string> scopeNames)
        {
            var scopes = scopeNames.ToArray();

            var identityResources = await IdentityDbContext.IdentityResources
                .Where(i => scopes.Contains(i.Name))
                .Include(x => x.UserClaims)
                .ToArrayAsync();

            return identityResources.Select(i => i.ToModel());
        }

        public async Task<IEnumerable<ApiResource>> FindApiResourcesByScopeAsync(IEnumerable<string> scopeNames)
        {
            var scopes = scopeNames.ToArray();

            var apiResources = await IdentityDbContext.ApiResources
                .Where(r => scopes.Contains(r.Name))
                .Include(x => x.UserClaims)
                .ToArrayAsync();

            return apiResources.Select(r => r.ToModel());
        }

        public async Task<ApiResource> FindApiResourceAsync(string name)
        {
            var apiResource = await IdentityDbContext.ApiResources
                .Where(r => r.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
                .Include(x => x.Secrets)
                .Include(x => x.Scopes)
                .ThenInclude(s => s.UserClaims)
                .Include(x => x.UserClaims)
                .FirstOrDefaultAsync();

            return apiResource?.ToModel();
        }

        public async Task<Resources> GetAllResources()
        {
            var identity = IdentityDbContext.IdentityResources
                .Include(x => x.UserClaims);

            var apis = IdentityDbContext.ApiResources
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
