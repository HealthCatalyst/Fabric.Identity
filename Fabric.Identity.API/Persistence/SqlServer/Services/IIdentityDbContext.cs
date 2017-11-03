using System.Threading.Tasks;
using Fabric.Identity.API.Persistence.SqlServer.Entities;
using Microsoft.EntityFrameworkCore;

namespace Fabric.Identity.API.Persistence.SqlServer.Services
{
    public interface IIdentityDbContext
    {
        DbSet<ClientEntity> Clients { get; set; }
        DbSet<ApiResourceEntity> ApiResources { get; set; }
        DbSet<IdentityResourceEntity> IdentityResources { get; set; }

        Task<int> SaveChangesAsync();
    }
}
