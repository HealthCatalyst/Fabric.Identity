using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Fabric.Identity.API.Models;
using Fabric.Identity.API.Services;
using Serilog;

namespace Fabric.Identity.API.DocumentDbStores
{
    public class DocumentDbUserStore : IUserStore
    {
        private readonly IDocumentDbService _documentDbService;
        private readonly ILogger _logger;

        public DocumentDbUserStore(IDocumentDbService documentDbService, ILogger logger)
        {
            _documentDbService = documentDbService;
            _logger = logger;
        }

        public Task<User> FindBySubjectId(string subjectId)
        {
            //store users by subjectId in couchDb ???

            throw new NotImplementedException();
        }

        public Task<User> FindByExternalProvider(string provider, string subjectId)
        {
            //userid is the sub from the claim or potentially the name identifier 
            throw new NotImplementedException();
        }

        public Task<User> ProvisionUser(string provider, string subjectId, IEnumerable<Claim> claims)
        {
            //create a new User and store the users claims

            throw new NotImplementedException();
        }
    }
}
