using System.Collections.Generic;
using System.Threading.Tasks;

namespace Fabric.Identity.API.Services
{
    public interface IDocumentDbService
    {
        Task<T> GetDocument<T>(string documentId);
        Task<IEnumerable<T>> GetDocumentsById<T>(IEnumerable<string> documentIds);
        Task<IEnumerable<T>> GetDocuments<T>(string documentKey);
        Task<int> GetDocumentCount(string documentType);
        void AddDocument<T>(string documentId, T documentObject);
        void UpdateDocument<T>(string documentId, T documentObject);
        void DeleteDocument<T>(string documentId);
    }
}