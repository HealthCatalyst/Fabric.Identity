using System;
using System.Linq;
using System.Threading.Tasks;
using Fabric.Identity.API.Models;
using Fabric.Identity.API.Services;
using Newtonsoft.Json;
using RestSharp.Extensions.MonoHttp;
using Serilog;

namespace Fabric.Identity.API.DocumentDbStores  
{
    public class DocumentDbUserStore
    {
        private readonly IDocumentDbService _documentDbService;
        private readonly ILogger _logger;

        public DocumentDbUserStore(IDocumentDbService documentDbService, ILogger logger)
        {
            _documentDbService = documentDbService;
            _logger = logger;
        }

        //id for a  user stored in documentDb = user:subjectid:provider
        private string GetUserDocumentId(string subjectId, string provider)
        {
            return $"{subjectId}:{provider}";
        }

        public async Task<User> FindBySubjectId(string subjectId)
        {
            var encodedSubjectId = UrlEncodeString(subjectId);
            _logger.Debug($"finding user with subject id: {encodedSubjectId}");
            var user = await _documentDbService.GetDocuments<User>($"{FabricIdentityConstants.DocumentTypes.UserDocumentType}{encodedSubjectId}");
            return user?.FirstOrDefault();
        }

        public async Task<User> FindByExternalProvider(string provider, string subjectId)
        {
            var encodedProvider = UrlEncodeString(provider);
            var encodedSubjectId = UrlEncodeString(subjectId);

            _logger.Debug($"finding user with subject id: {encodedSubjectId} and provider: {encodedProvider}");
            var user = await _documentDbService.GetDocuments<User>($"{FabricIdentityConstants.DocumentTypes.UserDocumentType}{GetUserDocumentId(subjectId, provider)}");
            
            return user?.FirstOrDefault();
        }

        public User AddUser(User user)
        {
            _documentDbService.AddDocument(GetUserDocumentId(user.SubjectId, user.ProviderName), user);
            _logger.Debug(
                $"added user: {user.SubjectId} with claims: {JsonConvert.SerializeObject(user.Claims?.Select(c => new {c.Type, c.Value}))}");
            return user;
        }

        public void UpdateUser(User user)
        {                    
            _documentDbService.UpdateDocument(GetUserDocumentId(user.SubjectId, user.ProviderName), user);
            _logger.Debug(
                $"updated user: {user.SubjectId} with claims: {JsonConvert.SerializeObject(user.Claims?.Select(c => new {c.Type, c.Value}))}");
        }

        private string UrlEncodeString(string unencoded)
        {
            return HttpUtility.UrlEncode(unencoded);
        }
    }
}
