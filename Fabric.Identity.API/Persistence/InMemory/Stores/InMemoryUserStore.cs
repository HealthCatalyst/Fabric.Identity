using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fabric.Identity.API.Models;

namespace Fabric.Identity.API.Persistence.InMemory.Stores
{
    public class InMemoryUserStore : IUserStore
    {
        private readonly IDocumentDbService _documentDbService;

        public InMemoryUserStore(IDocumentDbService documentDbService)
        {
            _documentDbService = documentDbService;
        }
        public async Task<User> FindBySubjectId(string subjectId)
        {
            var user = await _documentDbService.GetDocuments<User>(
                $"{FabricIdentityConstants.DocumentTypes.UserDocumentType}{subjectId.ToLower()}");
            return user?.FirstOrDefault();
        }

        public async Task<User> FindByExternalProvider(string provider, string subjectId)
        {
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
            return Task.FromResult(user);
        }

        public void UpdateUser(User user)
        {
            _documentDbService.UpdateDocument(GetUserDocumentId(user.SubjectId, user.ProviderName), user);
        }
        private static string GetUserDocumentId(string subjectId, string provider)
        {
            return $"{subjectId}:{provider}".ToLower();
        }
    }
}
