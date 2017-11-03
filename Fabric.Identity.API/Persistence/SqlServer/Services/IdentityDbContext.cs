using System.Threading.Tasks;
using Fabric.Identity.API.Persistence.SqlServer.Entities;
using Microsoft.EntityFrameworkCore;

namespace Fabric.Identity.API.Persistence.SqlServer.Services
{
    public class IdentityDbContext : DbContext, IIdentityDbContext
    {
        public DbSet<ClientEntity> Clients { get; set; }
        public DbSet<ApiResourceEntity> ApiResources { get; set; }
        public DbSet<IdentityResourceEntity> IdentityResources { get; set; }
        public DbSet<PersistedGrantEntity> PersistedGrants { get; set; }
        public DbSet<UserClaimEntity> UserClaims { get; set; }
        public DbSet<UserLoginEntity> UserLogins { get; set; }
        public DbSet<UserEntity> Users { get; set; }

        public Task<int> SaveChangesAsync()
        {
            return base.SaveChangesAsync();
        }
    }
}
