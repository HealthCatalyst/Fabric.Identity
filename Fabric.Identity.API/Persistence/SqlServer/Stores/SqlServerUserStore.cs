using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fabric.Identity.API.Persistence.SqlServer.EntityModels;
using Fabric.Identity.API.Persistence.SqlServer.Services;
using Microsoft.EntityFrameworkCore;
using Fabric.Identity.API.Persistence.SqlServer.Mappers;
using User = Fabric.Identity.API.Models.User;

namespace Fabric.Identity.API.Persistence.SqlServer.Stores
{
    public class SqlServerUserStore : IUserStore
    {
        private readonly IIdentityDbContext _identityDbContext;

        public SqlServerUserStore(IIdentityDbContext identityDbContext)
        {
            _identityDbContext = identityDbContext;
        }

        public async Task<User> FindBySubjectIdAsync(string subjectId)
        {
            var userEntity = await _identityDbContext.Users
                .Include(u => u.UserLogins)
                .Include(u => u.Claims)
                .FirstOrDefaultAsync(u => u.SubjectId.Equals(subjectId, StringComparison.OrdinalIgnoreCase));

            return userEntity.ToModel();
        }

        public async Task<User> FindByExternalProviderAsync(string provider, string subjectId)
        {
            var userEntity = await _identityDbContext.Users
                .Include(u => u.UserLogins)
                .Include(u => u.Claims)
                .FirstOrDefaultAsync(u => u.SubjectId.Equals(subjectId, StringComparison.OrdinalIgnoreCase)
                                          && u.ProviderName.Equals(provider, StringComparison.OrdinalIgnoreCase));

            return userEntity.ToModel();
        }

        public async Task<IEnumerable<User>> GetUsersBySubjectIdAsync(IEnumerable<string> subjectIds)
        {
            var userEntities = await _identityDbContext.Users
                .Where(u => subjectIds.Contains(u.ComputedUserId))
                .Include(u => u.UserLogins)
                .Include(u => u.Claims)
                .ToArrayAsync();

            return userEntities.Select(u => u.ToModel());
        }

        public async Task<User> AddUserAsync(User user)
        {
            var userEntity = user.ToEntity();

            foreach (var userLogin in user.LastLoginDatesByClient)
            {
                userEntity.UserLogins.Add(
                    new UserLogin {ClientId = userLogin.ClientId, LoginDate = userLogin.LoginDate});
            }

            foreach (var userClaim in user.Claims)
            {
                userEntity.Claims.Add(new UserClaim { Type = userClaim.Type });
            }

            _identityDbContext.Users.Add(userEntity);
            await _identityDbContext.SaveChangesAsync();

            return user;
        }

        public void UpdateUser(User user)
        {
            UpdateUserAsync(user).Wait();
        }

        public async Task UpdateUserAsync(User user)
        {
            var existingUser = await _identityDbContext.Users
                .Where(u => u.SubjectId.Equals(user.SubjectId, StringComparison.OrdinalIgnoreCase)
                            && u.ProviderName.Equals(user.ProviderName, StringComparison.OrdinalIgnoreCase))
                .Include(u => u.UserLogins)
                .Include(u => u.Claims)
                .FirstOrDefaultAsync();

            user.ToEntity(existingUser);

            foreach (var userLogin in user.LastLoginDatesByClient)
            {
                var existingLogin = existingUser.UserLogins.FirstOrDefault(l =>
                    l.ClientId.Equals(userLogin.ClientId, StringComparison.OrdinalIgnoreCase));

                if (existingLogin != null)
                {
                    existingLogin.LoginDate = userLogin.LoginDate;
                }
                else
                {
                    existingUser.UserLogins.Add(new UserLogin{ClientId = userLogin.ClientId, LoginDate = userLogin.LoginDate});
                }
                
            }

            foreach (var userClaim in user.Claims)
            {
                var existingClaim = existingUser.Claims.FirstOrDefault(l =>
                    l.Type.Equals(userClaim.Type, StringComparison.OrdinalIgnoreCase));

                if (existingClaim != null)
                {
                    existingClaim.Type = userClaim.Type;                    
                }
                else
                {
                    existingUser.Claims.Add(new UserClaim {Type = userClaim.Type});
                }

            }

            _identityDbContext.Users.Update(existingUser);
            await _identityDbContext.SaveChangesAsync();
        }
    }
}
