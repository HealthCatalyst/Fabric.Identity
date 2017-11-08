namespace Fabric.Identity.API.Persistence.SqlServer.EntityModels
{
    public class ApiScopeClaim
    {
        public int Id { get; set; }
        public int ApiScopeId { get; set; }
        public string Type { get; set; }

        public virtual ApiScope ApiScope { get; set; }
    }
}
