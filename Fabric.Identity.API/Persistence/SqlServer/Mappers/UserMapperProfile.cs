using System.Linq;
using System.Security.Claims;
using AutoMapper;
using Fabric.Identity.API.Persistence.SqlServer.EntityModels;

namespace Fabric.Identity.API.Persistence.SqlServer.Mappers
{
    public class UserMapperProfile : Profile
    {
        public UserMapperProfile()
        {
            //entity to model
            CreateMap<User, Models.User>(MemberList.Destination)
                .ForMember(x => x.LastLoginDatesByClient,
                    opt => opt.MapFrom(src => src.UserLogins
                        .Where(l => !l.IsDeleted)
                        .ToDictionary(l => l.Client.ClientId, l => l.LoginDate)))
                .ForMember(x => x.Claims, opt => opt.MapFrom(src => src.Claims
                    .Select(x => new Claim(x.Type, x.IdentityResource.Name))));

            //model to entity
            CreateMap<Models.User, User>(MemberList.Source)
                .ForMember(x => x.UserLogins, opt => opt.MapFrom(src => src.LastLoginDatesByClient
                    .Select(x => new UserLogin
                    {
                        //TODO: get the client id string and not the identity column
                    })));
        }

    }
}
