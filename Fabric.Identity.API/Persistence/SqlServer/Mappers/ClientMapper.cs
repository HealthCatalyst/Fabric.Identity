using AutoMapper;
using Entities = Fabric.Identity.API.Persistence.SqlServer.EntityModels;

namespace Fabric.Identity.API.Persistence.SqlServer.Mappers
{
    public static class ClientMapper
    {
        static ClientMapper()
        {
            Mapper = new MapperConfiguration(cfg => cfg.AddProfile<ClientMapperProfile>())
                .CreateMapper();
        }

        internal static IMapper Mapper { get; }

        /// <summary>
        /// Maps an entity to a model.
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        public static IdentityServer4.Models.Client ToModel(this Entities.Client entity)
        {
            return entity == null ? null : Mapper.Map<IdentityServer4.Models.Client>(entity);
        }

        /// <summary>
        /// Maps a model to an entity
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public static Entities.Client ToEntity(this IdentityServer4.Models.Client model)
        {
            return model == null ? null : Mapper.Map<Entities.Client>(model);
        }

        /// <summary>
        /// Maps a model to an existing entity instance
        /// </summary>
        /// <param name="model"></param>
        /// <param name="entity"></param>
        /// <returns></returns>
        public static void ToEntity(this IdentityServer4.Models.Client model, Entities.Client entity)
        {
            Mapper.Map(model, entity);
        }

        
    }
}
