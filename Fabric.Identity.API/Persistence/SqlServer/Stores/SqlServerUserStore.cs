using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fabric.Identity.API.Events;
using Fabric.Identity.API.Persistence.SqlServer.Services;
using Microsoft.EntityFrameworkCore;
using Fabric.Identity.API.Persistence.SqlServer.Mappers;
using Fabric.Identity.API.Services;
using IdentityServer4.Services;
using User = Fabric.Identity.API.Models.User;

namespace Fabric.Identity.API.Persistence.SqlServer.Stores
{
    public class SqlServerUserStore : SqlServerBaseStore, IUserStore
    {
        public SqlServerUserStore(IIdentityDbContext identityDbContext,
            IEventService eventService,
            IUserResolverService userResolverService,
            ISerializationSettings serializationSettings) : base(identityDbContext, eventService, userResolverService, serializationSettings)
        {
        }

        public async Task<User> FindBySubjectIdAsync(string subjectId)
        {
            var userEntity = await IdentityDbContext.Users
                .Include(u => u.UserLogins)
                .Include(u => u.Claims)
                .FirstOrDefaultAsync(u => u.SubjectId.Equals(subjectId, StringComparison.OrdinalIgnoreCase));

            var userModel = userEntity.ToModel();
            return userModel;
        }

        public async Task<User> FindByExternalProviderAsync(string provider, string subjectId)
        {
            var userEntity = await IdentityDbContext.Users
                .Include(u => u.UserLogins)
                .Include(u => u.Claims)
                .FirstOrDefaultAsync(u => u.SubjectId.Equals(subjectId, StringComparison.OrdinalIgnoreCase)
                                          && u.ProviderName.Equals(provider, StringComparison.OrdinalIgnoreCase));

            var userModel = userEntity.ToModel();
            return userModel;
        }

        public async Task<IEnumerable<User>> GetUsersBySubjectIdAsync(IEnumerable<string> subjectIds)
        {
            var userEntities = await IdentityDbContext.Users
                .Where(u => subjectIds.Contains(u.ComputedUserId))
                .Include(u => u.UserLogins)
                .Include(u => u.Claims)
                .ToArrayAsync();

            return userEntities.Select(u => u.ToModel());
        }

        public async Task<User> AddUserAsync(User user)
        {
            var userEntity = user.ToEntity();
            IdentityDbContext.Users.Add(userEntity);
            await IdentityDbContext.SaveChangesAsync();
            await EventService.RaiseAsync(
                new EntityCreatedAuditEvent<User>(
                    UserResolverService.Username,
                    UserResolverService.ClientId,
                    UserResolverService.Subject,
                    user.SubjectId,
                    user,
                    SerializationSettings));
            return user;
        }

        public void UpdateUser(User user)
        {
            UpdateUserAsync(user).Wait();
        }

        public async Task UpdateUserAsync(User user)
        {
            var existingUser = await IdentityDbContext.Users
                .Where(u => u.SubjectId.Equals(user.SubjectId, StringComparison.OrdinalIgnoreCase)
                            && u.ProviderName.Equals(user.ProviderName, StringComparison.OrdinalIgnoreCase))
                .Include(u => u.UserLogins)
                .Include(u => u.Claims)
                .FirstOrDefaultAsync();

            user.ToEntity(existingUser);

            IdentityDbContext.Users.Update(existingUser);
            await IdentityDbContext.SaveChangesAsync();
            await EventService.RaiseAsync(
                new EntityUpdatedAuditEvent<User>(
                    UserResolverService.Username,
                    UserResolverService.ClientId,
                    UserResolverService.Subject,
                    user.SubjectId,
                    user,
                    SerializationSettings));
        }
    }
}
