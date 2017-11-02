using System.Threading.Tasks;
using Fabric.Identity.API.Models.Domain;
using Microsoft.EntityFrameworkCore;

namespace Fabric.Identity.API.Persistence.SqlServer.Services
{
    public interface IIdentityDbContext
    {
        DbSet<ClientDomainModel> Clients { get; set; }

        Task<int> SaveChangesAsync();
    }
}
