using System.Linq;
using System.Security.Claims;
using Fabric.Identity.API.Models;
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

        public static UserEntity ToFabricEntity(this User user)
        {
            return new UserEntity
            {
                FirstName = user.FirstName,
                LastName = user.LastName,
                MiddleName = user.MiddleName,
                SubjectId = user.SubjectId,
                ProviderName = user.ProviderName,
                Username = user.Username,
                LastLoginDatesByClient =
                    user.LastLoginDatesByClient.Select(l => new UserLoginEntity(l.Key, l.Value)).ToList(),
                Claims = user.Claims.Select(c =>
                    new UserClaimEntity()
                    {
                        Type = c.Type,
                        IdentityResource = new IdentityResourceEntity
                        {
                            
                        }
                    }).ToList()
            };
        }

        public static User ToModel(this UserEntity userEntity)
        {
            return new User
            {
                FirstName = userEntity.FirstName,
                LastName = userEntity.LastName,
                MiddleName = userEntity.MiddleName,
                SubjectId = userEntity.SubjectId,
                ProviderName = userEntity.ProviderName,
                Username = userEntity.Username,
                Claims = userEntity.Claims.Select(c => new Claim(c.Type, c.IdentityResource.Name)).ToList(),
                LastLoginDatesByClient = userEntity.LastLoginDatesByClient.Where(l => !l.IsDeleted).ToDictionary(k => k.ClientId, v => v.LoginDate)
            };
        }
    }
}
