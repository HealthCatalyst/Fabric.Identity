namespace Fabric.Identity.API.Services
{
    public interface IExternalIdentityProviderServiceResolver
    {
        IExternalIdentityProviderService GetExternalIdentityProviderService(string identityProviderName);
    }
}