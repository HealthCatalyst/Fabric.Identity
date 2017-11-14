using AutoMapper;
using Fabric.Identity.API.Persistence.SqlServer.EntityModels;

namespace Fabric.Identity.API.Persistence.SqlServer.Mappers
{
    public static class ApiResourceMapper
    {
        static ApiResourceMapper()
        {
            Mapper = new MapperConfiguration(cfg => cfg.AddProfile<ApiResourceMapperProfile>())
                .CreateMapper();
        }

        internal static IMapper Mapper { get; }

        /// <summary>
        /// Maps an entity to a model.
        /// </summary>
        /// <param name="entity">The entity.</param>
        /// <returns></returns>
        public static IdentityServer4.Models.ApiResource ToModel(this ApiResource entity)
        {
            return entity == null ? null : Mapper.Map<IdentityServer4.Models.ApiResource>(entity);
        }

        /// <summary>
        /// Maps a model to an entity.
        /// </summary>
        /// <param name="model">The model.</param>
        /// <returns></returns>
        public static ApiResource ToEntity(this IdentityServer4.Models.ApiResource model)
        {
            return model == null ? null : Mapper.Map<ApiResource>(model);
        }

        /// <summary>
        /// Maps a model to an existing entity instance
        /// </summary>
        /// <param name="model"></param>
        /// <param name="entity"></param>
        /// <returns></returns>
        public static void ToEntity(this IdentityServer4.Models.ApiResource model, ApiResource entity)
        {
            Mapper.Map(model, entity);
        }
    }
}
