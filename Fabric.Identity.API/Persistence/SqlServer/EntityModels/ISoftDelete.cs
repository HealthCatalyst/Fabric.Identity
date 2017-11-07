namespace Fabric.Identity.API.Persistence.SqlServer.EntityModels
{
    public interface ISoftDelete
    {
        bool IsDeleted { get; set; }
    }
}
