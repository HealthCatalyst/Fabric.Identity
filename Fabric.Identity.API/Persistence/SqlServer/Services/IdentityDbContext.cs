using System.Threading.Tasks;
using Fabric.Identity.API.Persistence.SqlServer.EntityModels;
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

        public DbSet<Client> Clients { get; set; }
        public DbSet<ApiResource> ApiResources { get; set; }
        public DbSet<IdentityResource> IdentityResources { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<PersistedGrant> PersistedGrants { get; set; }

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
