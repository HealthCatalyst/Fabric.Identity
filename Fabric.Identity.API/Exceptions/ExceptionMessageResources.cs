namespace Fabric.Identity.API.Exceptions
{
    public static class ExceptionMessageResources
    {
        public static string MissingUserClaimMessage = "No User claim present on token";
        public static string MissingIssuerClaimMessage = "No Issuerer claim present on token";
        public static string ForbiddenIssuerMessageLog = "The Issuer on the claim ({0}) is not in the Issuer White List";
        public static string ForbiddenIssuerMessageUser = "Cannot access because the site is not configured for your account. Please contact your administrator for assistance";
    }
}
