using System;
using System.Collections.Generic;
using System.Linq;
using Fabric.Identity.API.Persistence.SqlServer.Mappers;
using IdentityServer4.Models;
using Serilog;

namespace Fabric.Identity.API.Persistence.SqlServer.Services
{
    public class SqlServerBootstrapper : IDbBootstrapper
    {
        private readonly IIdentityDbContext _identityDbContext;
        private readonly ILogger _logger;

        public SqlServerBootstrapper(IIdentityDbContext identityDbContext, ILogger logger)
        {
            _identityDbContext = identityDbContext;
            _logger = logger;
        }

        public bool Setup()
        {
            // TODO: generate DB here
            return true;
        }

        public async void AddResources(IEnumerable<IdentityResource> resources)
        {
            foreach (var identityResource in resources)
            {
                try
                {
                    var existingResource = _identityDbContext.IdentityResources.FirstOrDefault(i =>
                        i.Name.Equals(identityResource.Name, StringComparison.OrdinalIgnoreCase));

                    if (existingResource != null)
                    {
                        continue;
                    }

                    _identityDbContext.IdentityResources.Add(identityResource.ToEntity());
                }
                catch (Exception ex)
                {
                    _logger.Warning(ex, ex.Message);
                    throw;
                }
            }

            await _identityDbContext.SaveChangesAsync();
        }
    }
}