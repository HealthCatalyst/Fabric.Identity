using System.Threading.Tasks;
using Fabric.Identity.API.Persistence.SqlServer.Entities;
using IdentityServer4.EntityFramework.Options;
using Microsoft.EntityFrameworkCore;


namespace Fabric.Identity.API.Persistence.SqlServer.Services
{
    public class IdentityDbContext : DbContext, IIdentityDbContext
    {
        private readonly ConfigurationStoreOptions _storeOptions;

        public IdentityDbContext(DbContextOptions<IdentityDbContext> options, ConfigurationStoreOptions storeOptions)
            : base(options)
        {            
            _storeOptions = storeOptions;
        }

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

        

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ConfigureClientContext(_storeOptions);

            base.OnModelCreating(modelBuilder);
        }
    }
}
