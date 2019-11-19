namespace Fabric.Identity.API.Models
{
    public class FabricPrincipal
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string MiddleName { get; set; }
        public string SubjectId { get; set; }
        public string UserPrincipal { get; set; }
        public string ExternalIdentifier { get; set; }
        public string DisplayName { get; set; }
        public string IdentityProvider { get; set; }
        public string TenantId { get; set; }
        public string TenantAlias { get; set; }
        public FabricIdentityEnums.PrincipalType PrincipalType { get; set; }
        public string IdentityProviderUserPrincipalName { get; set; }
        public string Email { get; set; }

        public override string ToString()
        {
            return $"FirstName = {FirstName}, LastName = {LastName}, MiddleName = {MiddleName}, SubjectId = {SubjectId}, Email = {Email}";
        }
    }
}
