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

        public static IdentityServer4.Models.Client ToModel(this Entities.Client entity)
        {
            return Mapper.Map<IdentityServer4.Models.Client>(entity);
        }

        public static Entities.Client ToEntity(this IdentityServer4.Models.Client model)
        {
            return Mapper.Map<Entities.Client>(model);
        }
    }
}
