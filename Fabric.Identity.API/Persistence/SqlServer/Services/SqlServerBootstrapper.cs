using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using Fabric.Identity.API.Persistence.SqlServer.EntityModels;
using Fabric.Identity.API.Persistence.SqlServer.Mappers;
using Microsoft.EntityFrameworkCore;
using Serilog;
using IdentityResource = IdentityServer4.Models.IdentityResource;

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
            var existingResources = _identityDbContext.IdentityResources.Include(r => r.IdentityClaims).ToList();
            foreach (var identityResource in resources)
            {
                var existingResource = existingResources.FirstOrDefault(i =>
                    i.Name.Equals(identityResource.Name, StringComparison.OrdinalIgnoreCase));

                if (existingResource != null)
                {
                    var existingClaims = existingResource.IdentityClaims.ToList();
                    foreach (var identityResourceUserClaim in identityResource.UserClaims)
                    {
                        if (existingClaims.All(c => c.Type != identityResourceUserClaim))
                        {
                            existingResource.IdentityClaims.Add(new IdentityClaim {Type = identityResourceUserClaim});
                        }
                    }
                }
                else
                {
                    _identityDbContext.IdentityResources.Add(identityResource.ToEntity());
                }
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