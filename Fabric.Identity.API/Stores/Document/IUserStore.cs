using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Fabric.Identity.API.Models;

namespace Fabric.Identity.API.Stores.Document
{
    public interface IUserStore
    {
        Task<User> FindBySubjectId(string subjectId);
        Task<User> FindByExternalProvider(string provider, string subjectId);
        Task<User> AddUser(string provider, string subjectId, IEnumerable<Claim> claims);
    }
}