namespace Fabric.Identity.API.Persistence.SqlServer.Entities
{
    public interface ISoftDelete
    {
        bool IsDeleted { get; set; }
    }
}
