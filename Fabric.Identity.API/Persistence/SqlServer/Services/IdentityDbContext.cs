using System.Threading.Tasks;
using Fabric.Identity.API.Persistence.SqlServer.Models;
using Microsoft.EntityFrameworkCore;

namespace Fabric.Identity.API.Persistence.SqlServer.Services
{
    public class IdentityDbContext : DbContext, IIdentityDbContext
    {
        public DbSet<ClientDomainModel> Clients { get; set; }
        public DbSet<ApiResourceDomainModel> ApiResources { get; set; }
        public DbSet<IdentityResourceDomainModel> IdentityResources { get; set; }


        public Task<int> SaveChangesAsync()
        {
            return base.SaveChangesAsync();
        }
    }
}
