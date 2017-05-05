using System;

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
            AddDesignDocuments();
            AddClients();
            AddResources();
        }

        private void AddClients()
        {
            foreach (var client in Config.GetClients())
            {
                _documentDbService.AddOrUpdateDocument(client.ClientId, client);
            }
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

        private void AddDesignDocuments()
        {
            var couchDbAccessService = _documentDbService as CouchDbAccessService;

            if (couchDbAccessService == null)
            {
                throw new Exception("Invalid DocumentService provided to CoucbDbBootstrapper");
            }

            couchDbAccessService.AddOrUpdateDesignDocument("_design/client", ClientDesignDocumentJson);
            couchDbAccessService.AddOrUpdateDesignDocument("_design/resource", ResourcesDesignDocumentJson);
            couchDbAccessService.AddOrUpdateDesignDocument("_design/persistedgrant", PersistedGrantDesignDocumentJson);
        }
        
        private string ClientDesignDocumentJson =>
            @"{
                ""_id"": ""_design/client"",
                ""views"": {
                ""by_allowed_origin"": {
                    ""map"": ""function(doc){ if(doc.AllowedCorsOrigins.length > 0){ doc.AllowedCorsOrigins.forEach(function(origin){ emit(origin, 1);})}}""
                }
                }
            }";

        private string ResourcesDesignDocumentJson =>
            @"{
                ""_id"": ""_design/resource"",
                ""views"": {
                ""identity_resource"": {
                    ""map"": ""function(doc){ if(doc.Name && !doc.Scopes){ emit(doc.Name, 1);}}""
                },
                ""api_resource"": {
                    ""map"": ""function(doc){ if(doc.Scopes.length > 0){ doc.Scopes.forEach(function(scope){ emit(scope.Name, 1); });}}""
                }
                }
            }";

        private string PersistedGrantDesignDocumentJson =>
            @"{
	            ""_id"": ""_design/persistedgrant"",
	            ""views"": {
		            ""by_key"": {
			            ""map"": ""function(doc){ emit(doc.Key, 1); }""},
		            ""by_subjectId"" : {
			            ""map"": ""function(doc){ emit(doc.SubjectId, 1); }""},
		            ""by_subjectId_clientId"" : {
			            ""map"": ""function(doc){ emit([doc.SubjectId, doc.ClientId], 1); }""},
		            ""by_subjectId_clientId_type"": {
			            ""map"": ""function(doc){ emit([doc.SubjectId, doc.ClientId, doc.Type], 1); }""} 
	            }
            }"; 
    }
}
