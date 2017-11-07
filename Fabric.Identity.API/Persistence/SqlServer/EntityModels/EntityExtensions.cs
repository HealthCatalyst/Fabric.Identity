using System;
using System.Linq;
using System.Security.Claims;

namespace Fabric.Identity.API.Persistence.SqlServer.EntityModels
{
    public static class EntityExtensions
    {
        public static EntityModels.Client ToFabricEntity(this IdentityServer4.Models.Client is4Client)
        {
           throw new NotImplementedException();
        }

        public static EntityModels.ApiResource ToFabricEntity(this IdentityServer4.Models.ApiResource is4ApiResource)
        {
            throw new NotImplementedException();
        }

        public static EntityModels.IdentityResource ToFabricEntity(this IdentityServer4.Models.IdentityResource is4IdentityResource)
        {
            throw new NotImplementedException();
        }

        public static EntityModels.PersistedGrant ToFabricEntity(this IdentityServer4.Models.PersistedGrant is4PersistedGrant)
        {
            throw new NotImplementedException();
        }

        public static EntityModels.User ToFabricEntity(this Models.User user)
        {
            return new EntityModels.User
            {
                FirstName = user.FirstName,
                LastName = user.LastName,
                MiddleName = user.MiddleName,
                SubjectId = user.SubjectId,
                ProviderName = user.ProviderName,
                Username = user.Username,
                Claims = user.Claims.Select(c =>
                    new EntityModels.IdentityClaim
                    {
                        Type = c.Type,
                        IdentityResource = new EntityModels.IdentityResource
                        {
                            Name = c.Value
                        }
                    }).ToList()
            };
        }

        public static IdentityServer4.Models.Client ToModel(this EntityModels.Client clientEntity)
        {
            throw new NotImplementedException();
        }

        public static IdentityServer4.Models.IdentityResource ToModel(this EntityModels.IdentityResource identityResourceEntity)
        {
            throw new NotImplementedException();
        }

        public static IdentityServer4.Models.ApiResource ToModel(this EntityModels.ApiResource apiResourceEntity)
        {
            throw new NotImplementedException();
        }

        public static IdentityServer4.Models.PersistedGrant ToModel(this EntityModels.PersistedGrant persistedGrantEntity)
        {
            throw new NotImplementedException();
        }

        public static Models.User ToModel(this EntityModels.User userEntity)
        {
            return new Models.User
            {
                FirstName = userEntity.FirstName,
                LastName = userEntity.LastName,
                MiddleName = userEntity.MiddleName,
                SubjectId = userEntity.SubjectId,
                ProviderName = userEntity.ProviderName,
                Username = userEntity.Username,
                Claims = userEntity.Claims.Select(c => new Claim(c.Type, c.IdentityResource.Name)).ToList(),                
            };
        }
    }
}
