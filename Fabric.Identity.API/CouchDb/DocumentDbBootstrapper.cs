using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fabric.Identity.API.Services;

namespace Fabric.Identity.API.CouchDb
{
    public class DocumentDbBootstrapper
    {
        private readonly IDocumentDbService _documentDbService;

        public DocumentDbBootstrapper(IDocumentDbService documentDbService)
        {
            _documentDbService = documentDbService;
        }

        public virtual void Setup()
        {
            AddClients();
            AddResources();
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
