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
    public class SqlServerUserStore : IUserStore
    {
        private readonly IIdentityDbContext _identityDbContext;
        private readonly IUserResolverService _userResolverService;
        private readonly IEventService _eventService;
        private readonly ISerializationSettings _serializationSettings;

        public SqlServerUserStore(IIdentityDbContext identityDbContext,
            IEventService eventService,
            IUserResolverService userResolverService,
            ISerializationSettings serializationSettings)
        {
            _identityDbContext = identityDbContext;
            _eventService = eventService;
            _userResolverService = userResolverService;
            _serializationSettings = serializationSettings;
        }

        public async Task<User> FindBySubjectIdAsync(string subjectId)
        {
            var userEntity = await _identityDbContext.Users
                .Include(u => u.UserLogins)
                .Include(u => u.Claims)
                .FirstOrDefaultAsync(u => u.SubjectId.Equals(subjectId, StringComparison.OrdinalIgnoreCase));

            var userModel = userEntity.ToModel();
            return userModel;
        }

        public async Task<User> FindByExternalProviderAsync(string provider, string subjectId)
        {
            var userEntity = await _identityDbContext.Users
                .Include(u => u.UserLogins)
                .Include(u => u.Claims)
                .FirstOrDefaultAsync(u => u.SubjectId.Equals(subjectId, StringComparison.OrdinalIgnoreCase)
                                          && u.ProviderName.Equals(provider, StringComparison.OrdinalIgnoreCase));

            var userModel = userEntity.ToModel();
            return userModel;
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
            _identityDbContext.Users.Add(userEntity);
            await _identityDbContext.SaveChangesAsync();
            await _eventService.RaiseAsync(
                new EntityCreatedAuditEvent<User>(
                    _userResolverService.Username,
                    _userResolverService.ClientId,
                    _userResolverService.Subject,
                    user.SubjectId,
                    user,
                    _serializationSettings));
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

            _identityDbContext.Users.Update(existingUser);
            await _identityDbContext.SaveChangesAsync();
            await _eventService.RaiseAsync(
                new EntityUpdatedAuditEvent<User>(
                    _userResolverService.Username,
                    _userResolverService.ClientId,
                    _userResolverService.Subject,
                    user.SubjectId,
                    user,
                    _serializationSettings));
        }
    }
}
