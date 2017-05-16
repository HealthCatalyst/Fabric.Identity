using System;
using System.Linq;
using Fabric.Identity.API.Configuration;
using MyCouch;
using MyCouch.Net;

namespace Fabric.Identity.API.CouchDb
{
    public class CouchDbBootstrapper
    {
        private readonly IDocumentDbService _documentDbService;
        private readonly ICouchDbSettings _couchDbSettings;

        public CouchDbBootstrapper(IDocumentDbService documentDbService, ICouchDbSettings couchDbSettings)
        {
            _documentDbService = documentDbService;
            _couchDbSettings = couchDbSettings;
        }
        
        public void AddIdentityServiceArtifacts()
        {
            CreateDb();
            AddClients();
            AddResources();
        }

        private void CreateDb()
        {
            if (string.IsNullOrEmpty(_couchDbSettings.Username) ||
                string.IsNullOrEmpty(_couchDbSettings.Password))
                throw new Exception($"please add the admin username and password for the database to the CouchDbSettings in appsettings.json [DONT CHECK IT IN!!!]");
            
            var connectionInfo = new ServerConnectionInfo(_couchDbSettings.Server)
            {
                BasicAuth = new BasicAuthString(_couchDbSettings.Username, _couchDbSettings.Password)
            };

            using (var client = new MyCouchServerClient(connectionInfo))
            {
                var databaseInfo = client.Databases.HeadAsync(_couchDbSettings.DatabaseName).Result;

                if (databaseInfo.IsSuccess)
                {
                    var deleteResult = client.Databases.DeleteAsync(_couchDbSettings.DatabaseName).Result;

                    if (!deleteResult.IsSuccess)
                    {
                        throw new Exception($"unable to delete database: {_couchDbSettings.DatabaseName} reason: {deleteResult.Reason}");
                    }
                }

                var createResult = client.Databases.PutAsync(_couchDbSettings.DatabaseName).Result;

                if (!createResult.IsSuccess)
                {
                    throw new Exception($"unable to create database: {_couchDbSettings.DatabaseName} reason: {createResult.Reason}");
                }
            }
        }

        private void AddClients()
        {
            var clients = Config.GetClients().ToList();
            foreach (var client in clients)
            {
                _documentDbService.AddDocument(client.ClientId, client);
            }
        }

        private void AddResources()
        {
            var identityResources = Config.GetIdentityResources();
            foreach (var identityResource in identityResources)
            {
                _documentDbService.AddDocument(identityResource.Name, identityResource);
            }

            var apiResources = Config.GetApiResources();
            foreach (var apiResource in apiResources)
            {
                _documentDbService.AddDocument(apiResource.Name, apiResource);
            }
        }        
    }
}
