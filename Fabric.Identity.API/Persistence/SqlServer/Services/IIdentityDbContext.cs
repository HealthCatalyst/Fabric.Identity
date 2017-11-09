using System.Threading.Tasks;
using Fabric.Identity.API.Persistence.SqlServer.EntityModels;
using Microsoft.EntityFrameworkCore;

namespace Fabric.Identity.API.Persistence.SqlServer.Services
{
    public interface IIdentityDbContext
    {
        DbSet<Client> Clients { get; set; }
        DbSet<ApiResource> ApiResources { get; set; }
        DbSet<IdentityResource> IdentityResources { get; set; }        
        DbSet<User> Users { get; set; }
        DbSet<PersistedGrant> PersistedGrants { get; set; }

        Task<int> SaveChangesAsync();
        int SaveChanges();
    }
}
