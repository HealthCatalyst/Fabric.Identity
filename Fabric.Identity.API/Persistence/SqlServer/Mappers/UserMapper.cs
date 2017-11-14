using AutoMapper;
using Fabric.Identity.API.Models;

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
            return Mapper.Map<EntityModels.User>(model);
        }

        /// <summary>
        /// Maps a model to an existing entity instance
        /// </summary>
        /// <param name="model"></param>
        /// <param name="entity"></param>
        public static void ToEntity(this User model, EntityModels.User entity)
        {
            Mapper.Map(model, entity);
        }
    }
}
