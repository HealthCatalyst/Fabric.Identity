using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Fabric.Identity.API.Persistence.SqlServer.Services;
using IdentityServer4.Models;
using IdentityServer4.Stores;

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
            throw new NotImplementedException();
        }

        public void UpdateResource(string id, ApiResource resource)
        {
            throw new NotImplementedException();
        }

        public ApiResource GetResource(string id)
        {
            throw new NotImplementedException();
        }

        public void DeleteResource(string id)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<IdentityResource>> FindIdentityResourcesByScopeAsync(IEnumerable<string> scopeNames)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<ApiResource>> FindApiResourcesByScopeAsync(IEnumerable<string> scopeNames)
        {
            throw new NotImplementedException();
        }

        public Task<ApiResource> FindApiResourceAsync(string name)
        {
            throw new NotImplementedException();
        }

        public Task<Resources> GetAllResources()
        {
            throw new NotImplementedException();
        }
    }
}
