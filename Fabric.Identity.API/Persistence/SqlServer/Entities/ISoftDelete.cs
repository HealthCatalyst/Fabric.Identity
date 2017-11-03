namespace Fabric.Identity.API.Persistence.SqlServer.Models
{
    public interface ISoftDelete
    {
        bool IsDeleted { get; set; }
    }
}
