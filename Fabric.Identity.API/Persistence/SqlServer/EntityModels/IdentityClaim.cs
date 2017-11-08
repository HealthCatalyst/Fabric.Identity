namespace Fabric.Identity.API.Persistence.SqlServer.EntityModels
{
    public class IdentityClaim
    {
        public int Id { get; set; }
        public int IdentityResourceId { get; set; }
        public string Type { get; set; }

        public virtual IdentityResource IdentityResource { get; set; }
    }
}
