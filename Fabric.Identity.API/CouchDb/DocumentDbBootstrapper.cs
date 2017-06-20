using System;
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
            AddResources();
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
        }
    }
}
