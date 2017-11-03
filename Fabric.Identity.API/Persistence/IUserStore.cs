using System.Collections.Generic;
using System.Threading.Tasks;
using Fabric.Identity.API.Models;

namespace Fabric.Identity.API.Persistence
{
    public interface IUserStore
    {
        Task<User> FindBySubjectIdAsync(string subjectId);
        Task<User> FindByExternalProviderAsync(string provider, string subjectId);
        Task<IEnumerable<User>> GetUsersBySubjectIdAsync(IEnumerable<string> subjectIds);
        Task<User> AddUserAsync(User user);
        void UpdateUser(User user);

        Task UpdateUserAsync(User user);
    }
}