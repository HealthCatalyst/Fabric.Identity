using System;
using System.Net.Http;
using System.Net.Http.Headers;
using Fabric.Identity.API.CouchDb;
using Fabric.Identity.API.Persistence.Couchdb.Configuration;
using MyCouch;
using MyCouch.Net;
using MyCouch.Responses;
using Polly;
using Serilog;

namespace Fabric.Identity.API.Persistence.CouchDb.Configuration
{
    public class CouchDbBootstrapper : DocumentDbBootstrapper
    {
        private static readonly Policy CircuitBreaker =
            Policy.Handle<Exception>().CircuitBreaker(5, TimeSpan.FromMinutes(3));

        private readonly ICouchDbSettings _couchDbSettings;
        private readonly DbConnectionInfo _dbConnectionInfo;
        private readonly ILogger _logger;

        public CouchDbBootstrapper(IDocumentDbService documentDbService, ICouchDbSettings couchDbSettings,
            ILogger logger)
            : base(documentDbService)
        {
            _couchDbSettings = couchDbSettings;
            _dbConnectionInfo = MakeDbConnectionInfo();
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public override void Setup()
        {
            var dbCreated = CircuitBreaker.Execute(CreateDb);
            if (dbCreated)
            {
                CircuitBreaker.Execute(base.Setup);
            }
            CircuitBreaker.Execute(SetupDefaultUser);
            CircuitBreaker.Execute(SetupDesignDocuments);
        }

        private bool CreateDb()
        {
            if (string.IsNullOrEmpty(_couchDbSettings.Username) ||
                string.IsNullOrEmpty(_couchDbSettings.Password))
            {
                throw new CouchDbSetupException(
                    $"please add the admin username and password for the database to the CouchDbSettings in appsettings.json [DONT CHECK IT IN!!!]");
            }

            var connectionInfo = new ServerConnectionInfo(_couchDbSettings.Server)
            {
                BasicAuth = new BasicAuthString(_couchDbSettings.Username, _couchDbSettings.Password)
            };

            using (var client = new MyCouchServerClient(connectionInfo))
            {
                var databaseInfo = client.Databases.HeadAsync(_couchDbSettings.DatabaseName).Result;

                if (databaseInfo.IsSuccess)
                {
                    return false;
                }

                var createResult = client.Databases.PutAsync(_couchDbSettings.DatabaseName).Result;

                if (!createResult.IsSuccess)
                {
                    throw new CouchDbSetupException(
                        $"unable to create database: {_couchDbSettings.DatabaseName} reason: {createResult.Reason}");
                }
                return true;
            }
        }

        private void SetupDesignDocuments()
        {
            SetupDesignDocument(FabricIdentityConstants.FabricCouchDbDesignDocuments.Count,
                FabricIdentityConstants.FabricCouchDbDesignDocumentDefinitions.Count);
        }

        private void SetupDesignDocument(string designDocName, string designDocJson)
        {
            using (var client = new MyCouchClient(_dbConnectionInfo))
            {
                var documentId = $"_design/{designDocName}";
                var countDocument = client.Documents
                    .GetAsync(documentId)
                    .Result;

                DocumentHeaderResponse result;
                if (countDocument.IsSuccess)
                {
                    result = client.Documents.PutAsync(documentId, countDocument.Rev,
                            designDocJson)
                        .Result;
                    _logger.Information("{Count} design document is already created.",
                        designDocName);
                }
                else
                {
                    result = client.Documents.PostAsync(designDocJson).Result;
                }

                if (result.IsSuccess)
                {
                    return;
                }

                throw new CouchDbSetupException(
                    $"unable to create design document: {designDocName}, reason: {result.Reason}");
            }
        }

        private void SetupDefaultUser()
        {
            using (var httpClient = new HttpClient())
            {
                httpClient.BaseAddress = new Uri(_couchDbSettings.Server);
                httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue(HttpContentTypes.Json));
                httpClient.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Basic", _dbConnectionInfo.BasicAuth.Value);
                var response = httpClient.PutAsync($"{_couchDbSettings.DatabaseName}/_security",
                        GetCouchDbUserPayload())
                    .Result;

                if (!response.IsSuccessStatusCode)
                {
                    throw new CouchDbSetupException(
                        $"unable to create the {_couchDbSettings.DatabaseName}_user in {_couchDbSettings.DatabaseName}, response: {response.Content.ReadAsStringAsync().Result}, responseStatusCode: {response.StatusCode}.");
                }
            }
        }

        private DbConnectionInfo MakeDbConnectionInfo()
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

        private JsonContent GetCouchDbUserPayload()
        {
            return new JsonContent(
                $"{{\"admins\": {{ \"names\": [], \"roles\": [] }}, \"members\": {{ \"names\": [\"{_couchDbSettings.DatabaseName}_user\"], \"roles\": [] }} }}");
        }
    }
}