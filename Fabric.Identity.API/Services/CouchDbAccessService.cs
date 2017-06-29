using System.Collections.Generic;
using System.Threading.Tasks;
using Fabric.Identity.API.Configuration;
using MyCouch;
using MyCouch.Net;
using MyCouch.Requests;
using MyCouch.Responses;
using Newtonsoft.Json;
using Serilog;
using Fabric.Identity.API.CouchDb;

namespace Fabric.Identity.API.Services
{
    public class CouchDbAccessService : IDocumentDbService
    {
        private readonly ILogger _logger;
        private readonly ICouchDbSettings _couchDbSettings;
        private const string HighestUnicodeChar = "\ufff0";

        private string GetFullDocumentId<T>(string documentId)
        {
            return $"{typeof(T).Name.ToLowerInvariant()}:{documentId}";
        }

        private DbConnectionInfo DbConnectionInfo
        {
            get
            {
                var connectionInfo = new DbConnectionInfo(_couchDbSettings.Server, _couchDbSettings.DatabaseName);

                if (!string.IsNullOrEmpty(_couchDbSettings.Username) &&
                    !string.IsNullOrEmpty(_couchDbSettings.Password))
                {
                    connectionInfo.BasicAuth =
                        new BasicAuthString(_couchDbSettings.Username, _couchDbSettings.Password);
                }

                return connectionInfo;
            }
        }

        public CouchDbAccessService(ICouchDbSettings config, ILogger logger)
        {
            _couchDbSettings = config;
            _logger = logger;

            _logger.Debug(
                $"couchDb configuration properties: Server: {config.Server} -- DatabaseName: {config.DatabaseName}");
        }

        public Task<T> GetDocument<T>(string documentId)
        {

            using (var client = new MyCouchClient(DbConnectionInfo))
            {
                var documentResponse = client.Documents.GetAsync(GetFullDocumentId<T>(documentId)).Result;

                if (!documentResponse.IsSuccess)
                {
                    _logger.Debug($"unable to find document: {GetFullDocumentId<T>(documentId)} - message: {documentResponse.Reason}");
                    return Task.FromResult(default(T));
                }

                var json = documentResponse.Content;
                var document = JsonConvert.DeserializeObject<T>(json);

                return Task.FromResult(document);
            }
        }

        public Task<IEnumerable<T>> GetDocuments<T>(string documentType)
        {
            using (var client = new MyCouchClient(DbConnectionInfo))
            {
                var viewQuery = new QueryViewRequest(SystemViewIdentity.AllDocs)
                    .Configure(q => q.Reduce(false)
                        .IncludeDocs(true)
                        .StartKey(documentType)
                        .EndKey($"{documentType}{HighestUnicodeChar}"));

                ViewQueryResponse result = client.Views.QueryAsync(viewQuery).Result;

                if (!result.IsSuccess)
                {
                    _logger.Error($"unable to find documents for type: {documentType} - error: {result.Reason}");
                    return Task.FromResult(default(IEnumerable<T>));
                }

                var results = new List<T>();

                foreach (var responseRow in result.Rows)
                {
                    var resultRow = JsonConvert.DeserializeObject<T>(responseRow.IncludedDoc);
                    results.Add(resultRow);
                }

                return Task.FromResult((IEnumerable<T>)results);
            }
        }

        public Task<int> GetDocumentCount(string documentType)
        {
            using (var client = new MyCouchClient(DbConnectionInfo))
            {
                var viewQuery = new QueryViewRequest(FabricIdentityConstants.FabricCouchDbDesignDocuments.Count,
                    documentType);
                var result = client.Views.QueryAsync<int>(viewQuery).Result;
                if (result.Rows != null && result.Rows.Length > 0)
                {
                    return Task.FromResult(result.Rows[0].Value);
                }
                return Task.FromResult(0);
            }
        }

        public void AddDocument<T>(string documentId, T documentObject)
        {
            var fullDocumentId = GetFullDocumentId<T>(documentId);

            using (var client = new MyCouchClient(DbConnectionInfo))
            {
                var existingDoc = client.Documents.GetAsync(fullDocumentId).Result;
                var docJson = JsonConvert.SerializeObject(documentObject);

                if (!string.IsNullOrEmpty(existingDoc.Id))
                {
                    throw new ResourceOperationException($"Document with id {documentId} already exists.", ResourceOperationType.Add);
                }

                var response = client.Documents.PutAsync(fullDocumentId, docJson).Result;

                if (!response.IsSuccess)
                {
                    var message = $"unable to add document: {documentId} - error: {response.Reason}";
                    _logger.Error(message);
                    throw new ResourceOperationException(message, ResourceOperationType.Add);
                }
            }
        }

        public void UpdateDocument<T>(string documentId, T documentObject)
        {
            var fullDocumentId = GetFullDocumentId<T>(documentId);

            using (var client = new MyCouchClient(DbConnectionInfo))
            {
                var existingDoc = client.Documents.GetAsync(fullDocumentId).Result;
                var docJson = JsonConvert.SerializeObject(documentObject);

                if (existingDoc.IsEmpty)
                {
                    throw new ResourceOperationException($"Document with id {documentId} does not exist.", ResourceOperationType.Update);
                }

                var response = client.Documents.PutAsync(fullDocumentId, existingDoc.Rev, docJson).Result;

                if (!response.IsSuccess)
                {
                    var message = $"unable to update document: {documentId} - error: {response.Reason}";
                    _logger.Error(message);
                    throw new ResourceOperationException(message, ResourceOperationType.Update);
                }
            }
        }

        public void DeleteDocument<T>(string documentId)
        {
            using (var client = new MyCouchClient(DbConnectionInfo))
            {
                var documentResponse = client.Documents.GetAsync(GetFullDocumentId<T>(documentId)).Result;

                Delete(documentResponse.Id, documentResponse.Rev);
            }
        }

        private void Delete(string documentId, string rev)
        {
            using (var client = new MyCouchClient(DbConnectionInfo))
            {
                var response = client.Documents.DeleteAsync(documentId, rev).Result;

                if (!response.IsSuccess)
                {
                    var message = $"There was an error deleting document:{documentId}, error: {response.Reason}";
                    _logger.Error(message);
                    throw new ResourceOperationException(message, ResourceOperationType.Delete);
                }
            }
        }
    }
}