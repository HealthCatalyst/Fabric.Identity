namespace Fabric.Identity.API.Authorization
{
    public interface IHaveAuthorizationClaimType
    {
        string ClaimType { get; }
    }
}