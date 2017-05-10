using System.Collections.Generic;
using System.Linq;

namespace Fabric.Identity.API.CouchDb
{
    public class CouchDbBootstrapper
    {
        private readonly IDocumentDbService _documentDbService;

        public CouchDbBootstrapper(IDocumentDbService documentDbService)
        {
            _documentDbService = documentDbService;
        }
        
        public void AddIdentityServiceArtifacts()
        {
            AddClients();
            AddResources();
        }

        private void AddClients()
        {
            var clients = Config.GetClients().ToList();
            foreach (var client in clients)
            {
                _documentDbService.AddOrUpdateDocument(client.ClientId, client);
            }

            var allowedOrigins = clients
                .SelectMany(a => a.AllowedCorsOrigins).ToList();

            _documentDbService.AddOrUpdateDocument("allowedOrigins",
                new ClientOriginList {AllowedOrigins = allowedOrigins});
        }

        private void AddResources()
        {
            var identityResources = Config.GetIdentityResources();
            foreach (var identityResource in identityResources)
            {
                _documentDbService.AddOrUpdateDocument(identityResource.Name, identityResource);
            }

            var apiResources = Config.GetApiResources();
            foreach (var apiResource in apiResources)
            {
                _documentDbService.AddOrUpdateDocument(apiResource.Name, apiResource);
            }
        }        
    }

    public class ClientOriginList
    {
        public IList<string> AllowedOrigins { get; set; }
    }    

   

}
