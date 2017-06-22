namespace Fabric.Identity.API
{
    public static class FabricIdentityConstants
    {
        public static readonly string ServiceName = "identityservice";
        public static readonly string FabricCorsPolicyName = "FabricCorsPolicy";
        public static readonly string ManagementApiBasePath = "/api";
        public static readonly string IdentityRegistrationScope = "fabric/identity.manageresources";
        public static class DocumentTypes
        {
            public static readonly string IdentityResourceDocumentType = "identityresource:";
            public static readonly string ClientDocumentType = "client:";
            public static readonly string ApiResourceDocumentType = "apiresource:";
            public static readonly string PersistedGrantDocumentType = "persistedgrant:";
        }

        public static class FabricClaimTypes
        {
            public static readonly string Groups = "groups";
        }

        public static class FabricCouchDbDesignDocumentDefinitions
        {
            public static readonly string Count = @"{
                                                      ""_id"":""_design/count"",
                                                      ""language"": ""javascript"",
                                                      ""views"":
                                                      {
                                                        ""client:"": {
                                                          ""map"": ""function(doc) { if (doc._id.indexOf('client:') !== -1)  emit(doc.id, 1) }"",
                                                          ""reduce"" : ""_count""
                                                        },
                                                        ""apiresource:"": {
                                                          ""map"": ""function(doc) { if (doc._id.indexOf('apiresource:') !== -1)  emit(doc.id, 1) }"",
                                                          ""reduce"" : ""_count""
                                                        },
                                                        ""identityresource:"": {
                                                          ""map"": ""function(doc) { if (doc._id.indexOf('identityresource:') !== -1)  emit(doc.id, 1) }"",
                                                          ""reduce"" : ""_count""
                                                        }
                                                      }
                                                    }";
        }

        public static class FabricCouchDbDesignDocuments
        {
            public static readonly string Count = "count";
        }
    }
}
