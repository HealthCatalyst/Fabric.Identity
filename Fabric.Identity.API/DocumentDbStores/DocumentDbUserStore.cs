using System.Linq;
using System.Threading.Tasks;
using Fabric.Identity.API.Models;
using Fabric.Identity.API.Services;
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
            var user = await _documentDbService.GetDocuments<User>($"{FabricIdentityConstants.DocumentTypes.UserDocumentType}{encodedSubjectId}:{encodedProvider}");
            return user?.FirstOrDefault();
        }

        public User AddUser(User user)
        {
            var encodedProvider = UrlEncodeString(user.ProviderName);
            var encodedSubjectId = UrlEncodeString(user.SubjectId);
            _documentDbService.AddDocument($"{encodedSubjectId}:{encodedProvider}", user);
            _logger.Debug($"added user: {user.SubjectId}");

            return user;

        }

        public void UpdateUser(User user)
        {            
            var encodedSubjectId = UrlEncodeString(user.SubjectId);
            _documentDbService.UpdateDocument($"{encodedSubjectId}:{user.ProviderName}", user);
        }

        private string UrlEncodeString(string unencoded)
        {
            return HttpUtility.UrlEncode(unencoded);
        }
    }
}
