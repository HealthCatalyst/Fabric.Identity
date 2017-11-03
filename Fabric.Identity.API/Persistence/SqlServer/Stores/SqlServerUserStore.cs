using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fabric.Identity.API.Models;
using Fabric.Identity.API.Persistence.SqlServer.Services;

namespace Fabric.Identity.API.Persistence.SqlServer.Stores
{
    public class SqlServerUserStore : IUserStore
    {
        private readonly IIdentityDbContext _identityDbContext;

        public SqlServerUserStore(IIdentityDbContext identityDbContext)
        {
            _identityDbContext = identityDbContext;
        }

        public Task<User> FindBySubjectId(string subjectId)
        {
            throw new NotImplementedException();
        }

        public Task<User> FindByExternalProvider(string provider, string subjectId)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<User>> GetUsersBySubjectId(IEnumerable<string> subjectIds)
        {
            throw new NotImplementedException();
        }

        public Task<User> AddUser(User user)
        {
            throw new NotImplementedException();
        }

        public void UpdateUser(User user)
        {
            throw new NotImplementedException();
        }
    }
}
