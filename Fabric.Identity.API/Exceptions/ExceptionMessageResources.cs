namespace Fabric.Identity.API.Exceptions
{
    public static class ExceptionMessageResources
    {
        public static string MissingUserClaimMessage = "No User claim present on token";
        public static string MissingIssuerClaimMessage = "No Issuer claim present on token";
        public static string ForbiddenIssuerMessageLog = "The Issuer on the claim ({0}) is not in the Issuer White List";
        public static string ForbiddenIssuerMessageUser = "Access denied. Try closing the browser and logging in with a different user, or contact your administrator for assistance.";
    }
}
