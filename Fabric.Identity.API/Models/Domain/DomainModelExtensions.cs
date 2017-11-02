using IdentityServer4.EntityFramework.Mappers;

namespace Fabric.Identity.API.Models.Domain
{
    public static class DomainModelExtensions
    {
        public static ClientDomainModel ToDomainModel(this IdentityServer4.Models.Client is4Client)
        {
            var entityModel = is4Client.ToEntity();

            return (ClientDomainModel)entityModel;
        }
    }
}
