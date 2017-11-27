using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using Fabric.Identity.API.Persistence.SqlServer.Mappers;
using IdentityServer4.Models;
using Microsoft.EntityFrameworkCore;
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

        public void AddResources(IEnumerable<IdentityResource> resources)
        {
            var existingResources = _identityDbContext.IdentityResources.ToList();
            foreach (var identityResource in resources)
            {
                var existingResource = existingResources.FirstOrDefault(i =>
                    i.Name.Equals(identityResource.Name, StringComparison.OrdinalIgnoreCase));

                if (existingResource != null)
                {
                    continue;
                }

                _identityDbContext.IdentityResources.Add(identityResource.ToEntity());
            }

            try
            {
                _identityDbContext.SaveChanges();
            }
            catch (DbUpdateException ex)
            {
                _logger.Warning(ex, "Error when adding IdentityResource, error message: {Message}.", ex.Message);
            }
            catch (Exception ex)
            {
                _logger.Warning(ex, ex.Message);
                throw;
            }
        }
    }
}