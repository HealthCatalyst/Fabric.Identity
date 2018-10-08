namespace Fabric.Identity.API.Exceptions
{
    public static class ExceptionMessageResources
    {
        public static string MissingUserClaimMessage = "No User claim present on token";
        public static string MissingIssuerClaimMessage = "No Issuerer claim present on token";
        public static string ForbiddenIssuerMessage = "The Issuer on the claim ({0}) is not in the Issuer White List";
    }
}
