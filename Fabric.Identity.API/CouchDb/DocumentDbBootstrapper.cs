using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fabric.Identity.API.Services;

namespace Fabric.Identity.API.CouchDb
{
    [System.Obsolete]
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
                try
                {
                    _documentDbService.AddDocument(client.ClientId, client);
                }
                catch(Exception)
                {
                    //Deprecated code
                }
            }
        }

        private void AddResources()
        {
            var identityResources = Config.GetIdentityResources();
            foreach (var identityResource in identityResources)
            {
                try
                {
                    _documentDbService.AddDocument(identityResource.Name, identityResource);
                }
                catch (Exception)
                {
                    //Deprecated code
                }
            }

            var apiResources = Config.GetApiResources();
            foreach (var apiResource in apiResources)
            {
                try
                {
                    _documentDbService.AddDocument(apiResource.Name, apiResource);
                }
                catch (Exception)
                {
                    //Deprecated code
                }
            }
        }
    }
}
