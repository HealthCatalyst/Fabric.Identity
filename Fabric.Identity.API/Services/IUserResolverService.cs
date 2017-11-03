namespace Fabric.Identity.API.Services
{
    public interface IUserResolverService
    {
        string Username { get; }
        string ClientId { get; }
        string Subject { get; }
    }
}