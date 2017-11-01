namespace Fabric.Identity.API.Services
{
    public interface IUserResolveService
    {
        string Username { get; }
        string ClientId { get; }
        string Subject { get; }
    }
}