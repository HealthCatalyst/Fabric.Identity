using AutoMapper;
using Entities = Fabric.Identity.API.Persistence.SqlServer.EntityModels;

namespace Fabric.Identity.API.Persistence.SqlServer.Mappers
{
    public class PersistedGrantMapperProfile : Profile
    {       
        public PersistedGrantMapperProfile()
        {
            // entity to model
            CreateMap<Entities.PersistedGrant, IdentityServer4.Models.PersistedGrant>(MemberList.Destination);

            // model to entity
            CreateMap<IdentityServer4.Models.PersistedGrant, Entities.PersistedGrant>(MemberList.Source);
        }
    }
}
