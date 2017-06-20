namespace Fabric.Identity.API
{
    public static class FabricIdentityConstants
    {
        public static readonly string ServiceName = "identityservice";
        public static readonly string FabricCorsPolicyName = "FabricCorsPolicy";
        public static readonly string ManagementApiBasePath = "/api";
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
    }
}
