using System;

namespace Fabric.Identity.API.Persistence.CouchDb.Configuration
{
    public class DocumentDbBootstrapper
    {
        protected readonly IDocumentDbService DocumentDbService;

        public DocumentDbBootstrapper(IDocumentDbService documentDbService)
        {
            DocumentDbService = documentDbService;
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
                    DocumentDbService.AddDocument(identityResource.Name, identityResource);
                }
                catch (Exception)
                {
                    //Deprecated code
                }
            }
        }
    }
}