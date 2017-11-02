using IdentityServer4.EntityFramework.Mappers;

namespace Fabric.Identity.API.Persistence.SqlServer.Models
{
    public static class DomainModelExtensions
    {
        public static ClientDomainModel ToDomainModel(this IdentityServer4.Models.Client is4Client)
        {
            var entityModel = is4Client.ToEntity();
            return (ClientDomainModel)entityModel;
        }

        public static ApiResourceDomainModel ToDomainModel(this IdentityServer4.Models.ApiResource is4ApiResource)
        {
            var entityModel = is4ApiResource.ToEntity();
            return (ApiResourceDomainModel) entityModel;
        }

        public static IdentityResourceDomainModel ToDomainModel(this IdentityServer4.Models.IdentityResource is4IdentityResource)
        {
            var entityModel = is4IdentityResource.ToEntity();
            return (IdentityResourceDomainModel)entityModel;
        }
    }
}
