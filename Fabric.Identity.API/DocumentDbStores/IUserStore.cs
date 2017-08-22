using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Fabric.Identity.API.Models;

namespace Fabric.Identity.API.DocumentDbStores
{
    public interface IUserStore
    {
        Task<User> FindBySubjectId(string subjectId);
        Task<User> FindByExternalProvider(string provider, string userId);
        Task<User> AddUser(string provider, string userId, IEnumerable<Claim> claims);
    }
}
