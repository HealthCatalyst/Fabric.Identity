using AutoMapper;
using Fabric.Identity.API.Models;

namespace Fabric.Identity.API.Persistence.SqlServer.Mappers
{
    public static class UserMapper
    {
        static UserMapper()
        {
            Mapper = new MapperConfiguration(cfg => cfg.AddProfile<ClientMapperProfile>())
                .CreateMapper();
        }

        internal static IMapper Mapper { get; }

        public static User ToModel(this EntityModels.User user)
        {
            return Mapper.Map<User>(user);
        }

        public static EntityModels.User ToEntity(this User user)
        {
            return Mapper.Map<EntityModels.User>(user);
        }
    }
}
