using System.Threading.Tasks;

namespace Fabric.Identity.API.Infrastructure.Monitoring
{
    public interface IHealthCheckerService
    {
        Task<bool> CheckHealth();
    }
}