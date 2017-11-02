using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fabric.Identity.API.Models;
using Newtonsoft.Json;
using Serilog;

namespace Fabric.Identity.API.Persistence.CouchDb.Stores
{
    public class CouchDbUserStore : IUserStore
    {
        private readonly IDocumentDbService _documentDbService;
        private readonly ILogger _logger;

        public CouchDbUserStore(IDocumentDbService documentDbService, ILogger logger)
        {
            _documentDbService = documentDbService;
            _logger = logger;
        }

        public async Task<User> FindBySubjectId(string subjectId)
        {
            _logger.Debug($"finding user with subject id: {subjectId}");
            var user = await _documentDbService.GetDocuments<User>(
                $"{FabricIdentityConstants.DocumentTypes.UserDocumentType}{subjectId.ToLower()}");
            return user?.FirstOrDefault();
        }

        public async Task<User> FindByExternalProvider(string provider, string subjectId)
        {
            _logger.Debug($"finding user with subject id: {subjectId} and provider: {provider}");
            var user = await _documentDbService.GetDocuments<User>(
                $"{FabricIdentityConstants.DocumentTypes.UserDocumentType}{GetUserDocumentId(subjectId, provider)}");

            return user?.FirstOrDefault();
        }

        public Task<IEnumerable<User>> GetUsersBySubjectId(IEnumerable<string> subjectIds)
        {
            return _documentDbService.GetDocumentsById<User>(subjectIds);
        }

        public Task<User> AddUser(User user)
        {
            _documentDbService.AddDocument(GetUserDocumentId(user.SubjectId, user.ProviderName), user);
            _logger.Debug(
                $"added user: {user.SubjectId} with claims: {JsonConvert.SerializeObject(user.Claims?.Select(c => new {c.Type, c.Value}))}");
            return Task.FromResult(user);
        }

        public void UpdateUser(User user)
        {
            _documentDbService.UpdateDocument(GetUserDocumentId(user.SubjectId, user.ProviderName), user);
            _logger.Debug(
                $"updated user: {user.SubjectId} with claims: {JsonConvert.SerializeObject(user.Claims?.Select(c => new {c.Type, c.Value}))}");
        }

        //id for a  user stored in documentDb = user:subjectid:provider
        private static string GetUserDocumentId(string subjectId, string provider)
        {
            return $"{subjectId}:{provider}".ToLower();
        }
    }
}