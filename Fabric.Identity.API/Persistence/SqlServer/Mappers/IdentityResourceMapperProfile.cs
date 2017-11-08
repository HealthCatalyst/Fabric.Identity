using System.Linq;
using AutoMapper;
using IdentityResource = Fabric.Identity.API.Persistence.SqlServer.EntityModels.IdentityResource; 

namespace Fabric.Identity.API.Persistence.SqlServer.Mappers
{
    public class IdentityResourceMapperProfile : Profile
    {
        public IdentityResourceMapperProfile()
        {
            // entity to model
            CreateMap<IdentityResource, IdentityServer4.Models.IdentityResource>(MemberList.Destination)
                .ConstructUsing(src => new IdentityServer4.Models.IdentityResource())
                .ForMember(x => x.UserClaims, opt => opt.MapFrom(src => src.IdentityClaims.Select(x => x)));

            // model to entity
            CreateMap<IdentityServer4.Models.IdentityResource, IdentityResource>(MemberList.Source)
                .ForMember(x => x.IdentityClaims, opts => opts.MapFrom(src => src.UserClaims.Select(x => x)));
        }
    }
}
