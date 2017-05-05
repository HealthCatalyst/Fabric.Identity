using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Fabric.Identity.API.Configuration;
using MyCouch;
using MyCouch.Net;
using MyCouch.Requests;
using MyCouch.Responses;
using Newtonsoft.Json;

namespace Fabric.Identity.API.CouchDb
{
    public interface IDocumentDbService
    {
        Task<T> GetDocument<T>(string documentId);
        void AddDocument<T>(string documentId, T documentObject);
        Task<bool> DoesDocumentExist(string typeName, string[] key);
        Task<T> FindDocumentByKey<T>(string typeName, string[] key);
        Task<IEnumerable<T>> FindDocumentsByKey<T>(string typeName, string[] key);
        Task<IEnumerable<T>> FindDocumentsByKeys<T>(string typeName, IEnumerable<string> keys);
        Task<IEnumerable<T>> FindDocuments<T>(string typeName);
        void DeleteDocument(string typeName, string[] key);
    }

    public class CouchDbAccessService : IDocumentDbService
    {
        private readonly Serilog.ILogger _logger;
        private readonly ICouchDbSettings _couchDbSettings;
        private string BaseDbUrl => $"{_couchDbSettings.Server}{_couchDbSettings.DatabaseName}/";
        private string GetDocumentUrl(string documentId)
        {
            return $"{BaseDbUrl}{documentId}";
        }

        private class CouchDbViewQuery
        {
            public string DesignDocument { get; }
            public string ViewName { get; }
            public bool IsComplexKey { get; }

            public CouchDbViewQuery(string designDocument, string viewName, bool isComplexKey)
            {
                DesignDocument = designDocument;
                ViewName = viewName;
                IsComplexKey = isComplexKey;
            }            
        }

        private Dictionary<string, CouchDbViewQuery> ViewMapping =>
            new Dictionary<string, CouchDbViewQuery>
            {
                {"client", new CouchDbViewQuery("client", "by_allowed_origin", false)},
                {"apiresource", new CouchDbViewQuery("resource", "api_resource", false)},
                {"identityresource", new CouchDbViewQuery("resource", "identity_resource", false)},
                {"persistedgrant", new CouchDbViewQuery("persistedgrant", "by_key", false) },
                {"persistedgrantsubject", new CouchDbViewQuery("persistedgrant", "by_subjecId", true) },
                {"persistedgrantsubjectclient", new CouchDbViewQuery("persistedgrant", "by_subjectId_clientId", true)},
                {"persistedgrantsubjectclienttype", new CouchDbViewQuery("persistedgrant", "by_subjectId_clientId_type", true)},
            };

        public CouchDbAccessService(ICouchDbSettings config, Serilog.ILogger logger)
        {
            _couchDbSettings = config;
            _logger = logger;

            _logger.Debug(
                $"couchDb configuration properties: Server: {config.Server} -- DatabaseName: {config.DatabaseName}");
        }

        public Task<T> GetDocument<T>(string documentId)
        {
            HttpClient webClient = new HttpClient();
            var response = webClient.GetAsync(GetDocumentUrl(documentId)).Result;

            if (!response.IsSuccessStatusCode)
            {
                return Task.FromResult(default(T));
            }

            var json = response.Content.ReadAsStringAsync().Result;
            var client = JsonConvert.DeserializeObject<T>(json);

            return Task.FromResult(client);
        }

        public void AddDocument<T>(string documentId, T documentObject)
        {
            if (GetDocument<T>(documentId).Result != null)
                return;

            HttpClient webClient = new HttpClient();

            var postData = JsonConvert.SerializeObject(documentObject);
            var content = new StringContent(postData, Encoding.UTF8, "application/json");
            var response = webClient.PutAsync(GetDocumentUrl(documentId), content).Result;

            if (!response.IsSuccessStatusCode)
            {
                _logger.Error($"could not add document to couchdb: documentId={documentId}");
            }
        }

        public Task<bool> DoesDocumentExist(string typeName, string[] key)
        {
            var viewMapping = ViewMapping[typeName];

            var response = QueryByView(viewMapping, key);

            return Task.FromResult(response.RowCount > 0);
        }

        public Task<T> FindDocumentByKey<T>(string typeName, string[] key)
        {
            var viewMapping = ViewMapping[typeName];

            var response = QueryByView(viewMapping, key);

            var resultRow = response.Rows.FirstOrDefault();
            var result = JsonConvert.DeserializeObject<T>(resultRow.IncludedDoc);

            return Task.FromResult(result);
        }

        public Task<IEnumerable<T>> FindDocumentsByKey<T>(string typeName, string[] key)
        {
            var viewMapping = ViewMapping[typeName];

            var response = QueryByView(viewMapping, key);
            var results = new List<T>();

            foreach (var responseRow in response.Rows)
            {
                var resultRow = JsonConvert.DeserializeObject<T>(responseRow.IncludedDoc);
                results.Add(resultRow);
            }

            return Task.FromResult((IEnumerable<T>)results);
        }

        public Task<IEnumerable<T>> FindDocumentsByKeys<T>(string typeName, IEnumerable<string> keys)
        {
            var viewMapping = ViewMapping[typeName];

            var response = QueryByView(viewMapping, keys);
            var results = new List<T>();

            foreach (var responseRow in response.Rows)
            {
                var resultRow = JsonConvert.DeserializeObject<T>(responseRow.IncludedDoc);
                results.Add(resultRow);
            }

            return Task.FromResult((IEnumerable<T>) results);
        }

        public Task<IEnumerable<T>> FindDocuments<T>(string typeName)
        {
            var viewMapping = ViewMapping[typeName];

            var response = QueryByView(viewMapping);
            var results = new List<T>();

            foreach (var responseRow in response.Rows)
            {
                var resultRow = JsonConvert.DeserializeObject<T>(responseRow.IncludedDoc);
                results.Add(resultRow);
            }

            return Task.FromResult((IEnumerable<T>)results);
        }

        public void DeleteDocument(string typeName, string[] key)
        {
            var viewMapping = ViewMapping[typeName];

            var getResponse = QueryByView(viewMapping, key);
            var rowInfo = JsonConvert.DeserializeObject<dynamic>(getResponse.Rows.First().IncludedDoc);

            Delete(rowInfo._id, rowInfo._rev);
        }

        private ViewQueryResponse QueryByView(CouchDbViewQuery query, string[] key)
        {
            using (var client = new MyCouchClient(_couchDbSettings.Server, _couchDbSettings.DatabaseName))
            {
                var viewQuery = new QueryViewRequest(query.DesignDocument, query.ViewName)
                    .Configure(q => q.Reduce(false).IncludeDocs(true));

                viewQuery = query.IsComplexKey
                    ? viewQuery.Configure(q => q.StartKey(key).EndKey(key))
                    : viewQuery.Configure(q => q.Key(key.First()));

                ViewQueryResponse result = client.Views.QueryAsync(viewQuery).Result;

                Console.WriteLine($"querying {query.ViewName} with key(s): {string.Join(",",key)}. result count: {result.RowCount}");

                return result;
            }
        }

        private ViewQueryResponse QueryByView(CouchDbViewQuery query, IEnumerable<string> keys)
        {
            using (var client = new MyCouchClient(_couchDbSettings.Server, _couchDbSettings.DatabaseName))
            {
                var viewQuery = new QueryViewRequest(query.DesignDocument, query.ViewName)
                    .Configure(q => q.Reduce(false).IncludeDocs(true).Keys(keys.ToArray()));

                ViewQueryResponse result = client.Views.QueryAsync(viewQuery).Result;

                return result;
            }
        }

        private ViewQueryResponse QueryByView(CouchDbViewQuery query)
        {
            using (var client = new MyCouchClient(_couchDbSettings.Server, _couchDbSettings.DatabaseName))
            {
                var viewQuery = new QueryViewRequest(query.DesignDocument, query.ViewName)
                    .Configure(q => q.Reduce(false).IncludeDocs(true));

                ViewQueryResponse result = client.Views.QueryAsync(viewQuery).Result;

                return result;
            }
        }

        private void Delete(string documentId, string rev)
        {
            using (var client = new MyCouchClient(_couchDbSettings.Server, _couchDbSettings.DatabaseName))
            {
                var response = client.Documents.DeleteAsync(documentId, rev).Result;

                if (!response.IsSuccess)
                {
                    throw new Exception($"There was an error deleting document:{documentId}, error: {response.Reason}");
                }
            }
        }

        public void AddOrUpdateDesignDocument(string id, string json)
        {
            var dbInfo =
                new DbConnectionInfo(_couchDbSettings.Server, _couchDbSettings.DatabaseName)
                {
                    BasicAuth = new BasicAuthString("admin", "admin")
                };

            using (var client = new MyCouchClient(dbInfo))
            {
                var existingDoc = client.Documents.GetAsync(id).Result;
                
                var response = !existingDoc.IsEmpty
                    ? client.Documents.PutAsync(id, existingDoc.Rev, json).Result
                    : client.Documents.PostAsync(json).Result;
                
                if (!response.IsSuccess)
                {
                    throw new Exception(
                        $"Design Document was not added or updated successfully. Document ID: {id}, Error: {response.Error}, Reason: {response.Reason}");
                }
            }
        }
    }
}