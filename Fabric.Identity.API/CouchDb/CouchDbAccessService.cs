using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Fabric.Identity.API.Configuration;
using Newtonsoft.Json;

namespace Fabric.Identity.API.CouchDb
{
    public interface IDocumentDbService
    {
        Task<T> GetDocument<T>(string documentId);
        void AddDocument<T>(string documentId, T documentObject);
    }

    public class CouchDbAccessService : IDocumentDbService
    {
        private readonly Serilog.ILogger _logger;
        private readonly ICouchDbSettings _couchDbSettings;

        private string GetDocumentUrl(string documentId)
        {
            return $"{_couchDbSettings.Server}{_couchDbSettings.DatabaseName}/{documentId}";
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
    }
}