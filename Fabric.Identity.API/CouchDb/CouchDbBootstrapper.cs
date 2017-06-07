using System;
using System.Linq;
using Fabric.Identity.API.Configuration;
using Fabric.Identity.API.Services;
using MyCouch;
using MyCouch.Net;
using Polly;

namespace Fabric.Identity.API.CouchDb
{
    public class CouchDbBootstrapper : DocumentDbBootstrapper
    {
        private readonly ICouchDbSettings _couchDbSettings;
        private static readonly Policy CircuitBreaker = Policy.Handle<Exception>().CircuitBreaker(5, TimeSpan.FromMinutes(3));

        public CouchDbBootstrapper(IDocumentDbService documentDbService, ICouchDbSettings couchDbSettings)
            : base(documentDbService)
        {
            _couchDbSettings = couchDbSettings;
        }

        public override void Setup()
        {
            CircuitBreaker.Execute(CreateDb);
            CircuitBreaker.Execute(base.Setup);
        }

        private void CreateDb()
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
                    var deleteResult = client.Databases.DeleteAsync(_couchDbSettings.DatabaseName).Result;

                    if (!deleteResult.IsSuccess)
                    {
                        throw new CouchDbSetupException($"unable to delete database: {_couchDbSettings.DatabaseName} reason: {deleteResult.Reason}");
                    }
                }

                var createResult = client.Databases.PutAsync(_couchDbSettings.DatabaseName).Result;

                if (!createResult.IsSuccess)
                {
                    throw new CouchDbSetupException($"unable to create database: {_couchDbSettings.DatabaseName} reason: {createResult.Reason}");
                }
            }
        }


    }
}
