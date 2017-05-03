using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Fabric.Identity.API.Configuration;
using MyCouch;
using MyCouch.Requests;
using MyCouch.Responses;
using Newtonsoft.Json;

namespace Fabric.Identity.API.CouchDb
{
    public interface IDocumentDbService
    {
        Task<T> GetDocument<T>(string documentId);
        void AddDocument<T>(string documentId, T documentObject);
        Task<bool> DoesDocumentExist(string typeName, string key);
        Task<IEnumerable<T>> FindDocumentsByKeys<T>(string typeName, IEnumerable<string> keys);
        Task<IEnumerable<T>> FindDocuments<T>(string typeName);
    }

    public class CouchDbAccessService : IDocumentDbService
    {
        private readonly Serilog.ILogger _logger;
        private readonly ICouchDbSettings _couchDbSettings;

        private Dictionary<string, Tuple<string, string>> ViewMapping =>
            new Dictionary<string, Tuple<string, string>>
            {
                {"client", new Tuple<string, string>("client", "by_allowed_origin")},
                {"apiresource", new Tuple<string, string>("resource", "api_resource")},
                {"identityresource", new Tuple<string, string>("resource", "identity_resource")}
            };

        private string BaseDbUrl => $"{_couchDbSettings.Server}{_couchDbSettings.DatabaseName}/";
        private string GetDocumentUrl(string documentId)
        {
            return $"{BaseDbUrl}{documentId}";
        }

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

        public Task<bool> DoesDocumentExist(string typeName, string key)
        {
            var viewMapping = ViewMapping[typeName];

            var response = QueryByView(viewMapping.Item1, viewMapping.Item2, key);

            return Task.FromResult(response.RowCount > 0);
        }

        public Task<IEnumerable<T>> FindDocumentsByKeys<T>(string typeName, IEnumerable<string> keys)
        {
            var viewMapping = ViewMapping[typeName];

            var response = QueryByView(viewMapping.Item1, viewMapping.Item2, keys);
            var results = new List<T>();

            foreach (var responseRow in response.Rows)
            {
                var resultRow = JsonConvert.DeserializeObject<T>(responseRow.Value);
                results.Add(resultRow);
            }

            return Task.FromResult((IEnumerable<T>) results);
        }

        public Task<IEnumerable<T>> FindDocuments<T>(string typeName)
        {
            var viewMapping = ViewMapping[typeName];

            var response = QueryByView(viewMapping.Item1, viewMapping.Item2);
            var results = new List<T>();

            foreach (var responseRow in response.Rows)
            {
                var resultRow = JsonConvert.DeserializeObject<T>(responseRow.Value);
                results.Add(resultRow);
            }

            return Task.FromResult((IEnumerable<T>)results);
        }

        private ViewQueryResponse QueryByView(string designDocName, string viewName, string key)
        {
            using (var client = new MyCouchClient(_couchDbSettings.Server, _couchDbSettings.DatabaseName))
            {
                var viewQuery = new QueryViewRequest(designDocName, viewName)
                    .Configure(q => q.Reduce(false));

                if (!string.IsNullOrEmpty(key))
                {
                    viewQuery = viewQuery.Configure(q => q.Key(key));
                }

                ViewQueryResponse result = client.Views.QueryAsync(viewQuery).Result;

                return result;
            }
        }

        private ViewQueryResponse QueryByView(string designDocName, string viewName, IEnumerable<string> keys)
        {
            using (var client = new MyCouchClient(_couchDbSettings.Server, _couchDbSettings.DatabaseName))
            {
                var viewQuery = new QueryViewRequest(designDocName, viewName)
                    .Configure(q => q.Reduce(false).Keys(keys.ToArray()));

                ViewQueryResponse result = client.Views.QueryAsync(viewQuery).Result;

                return result;
            }
        }

        private ViewQueryResponse QueryByView(string designDocName, string viewName)
        {
            using (var client = new MyCouchClient(_couchDbSettings.Server, _couchDbSettings.DatabaseName))
            {
                var viewQuery = new QueryViewRequest(designDocName, viewName)
                    .Configure(q => q.Reduce(false));

                ViewQueryResponse result = client.Views.QueryAsync(viewQuery).Result;

                return result;
            }
        }
    }
}