using System.Threading.Tasks;
using Fabric.Identity.API.Models;

namespace Fabric.Identity.API.Persistence
{
    public interface IUserStore
    {
        Task<User> FindBySubjectId(string subjectId);
        Task<User> FindByExternalProvider(string provider, string subjectId);
        Task<User> AddUser(User user);
        void UpdateUser(User user);
    }
}