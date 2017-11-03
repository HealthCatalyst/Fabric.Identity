using IdentityServer4.EntityFramework.Mappers;

namespace Fabric.Identity.API.Persistence.SqlServer.Entities
{
    public static class EntityExtensions
    {
        public static ClientEntity ToFabricEntity(this IdentityServer4.Models.Client is4Client)
        {
            var entityModel = is4Client.ToEntity();
            return (ClientEntity)entityModel;
        }

        public static ApiResourceEntity ToFabricEntity(this IdentityServer4.Models.ApiResource is4ApiResource)
        {
            var entityModel = is4ApiResource.ToEntity();
            return (ApiResourceEntity) entityModel;
        }

        public static IdentityResourceEntity ToFabricEntity(this IdentityServer4.Models.IdentityResource is4IdentityResource)
        {
            var entityModel = is4IdentityResource.ToEntity();
            return (IdentityResourceEntity)entityModel;
        }
    }
}
