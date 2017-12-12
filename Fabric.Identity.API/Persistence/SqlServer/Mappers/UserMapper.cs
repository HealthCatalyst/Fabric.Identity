using System;
using System.Linq;
using AutoMapper;
using Fabric.Identity.API.Persistence.SqlServer.EntityModels;
using User = Fabric.Identity.API.Models.User;
using UserLogin = Fabric.Identity.API.Persistence.SqlServer.EntityModels.UserLogin;

namespace Fabric.Identity.API.Persistence.SqlServer.Mappers
{
    public static class UserMapper
    {
        static UserMapper()
        {
            Mapper = new MapperConfiguration(cfg => cfg.AddProfile<UserMapperProfile>())
                .CreateMapper();
        }

        internal static IMapper Mapper { get; }

        /// <summary>
        /// Maps an entity to a model
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        public static User ToModel(this EntityModels.User entity)
        {
            return Mapper.Map<User>(entity);
        }

        /// <summary>
        /// Maps a model to an entity 
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public static EntityModels.User ToEntity(this User model)
        {
            var userEntity = Mapper.Map<EntityModels.User>(model);

            foreach (var userLogin in model.LastLoginDatesByClient)
            {
                userEntity.UserLogins.Add(
                    new UserLogin { ClientId = userLogin.ClientId, LoginDate = userLogin.LoginDate });
            }

            foreach (var userClaim in model.Claims)
            {
                userEntity.Claims.Add(new UserClaim { Type = userClaim.Type });
            }

            return userEntity;
        }

        /// <summary>
        /// Maps a model to an existing entity instance
        /// </summary>
        /// <param name="model"></param>
        /// <param name="entity"></param>
        public static void ToEntity(this User model, EntityModels.User entity)
        {
            Mapper.Map(model, entity);

            foreach (var userLogin in model.LastLoginDatesByClient)
            {
                var existingLogin = entity.UserLogins.FirstOrDefault(l =>
                    l.ClientId.Equals(userLogin.ClientId, StringComparison.OrdinalIgnoreCase));

                if (existingLogin != null)
                {
                    existingLogin.LoginDate = userLogin.LoginDate;
                }
                else
                {
                    entity.UserLogins.Add(new UserLogin { ClientId = userLogin.ClientId, LoginDate = userLogin.LoginDate });
                }

            }

            foreach (var userClaim in model.Claims)
            {
                var existingClaim = entity.Claims.FirstOrDefault(l =>
                    l.Type.Equals(userClaim.Type, StringComparison.OrdinalIgnoreCase));

                if (existingClaim != null)
                {
                    existingClaim.Type = userClaim.Type;
                }
                else
                {
                    entity.Claims.Add(new UserClaim { Type = userClaim.Type });
                }

            }
        }
    }
}
