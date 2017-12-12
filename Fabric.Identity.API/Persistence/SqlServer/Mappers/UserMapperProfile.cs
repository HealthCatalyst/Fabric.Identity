using System;
using System.Collections.Generic;
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
                .ForMember(x => x.LastLoginDatesByClient, opt => opt.MapFrom(src => src.UserLogins))
                .ForMember(x => x.Claims, opt => opt.MapFrom(src => src.Claims));


            //model to entity
            CreateMap<Models.User, User>(MemberList.Source)
                .ForMember(x => x.UserLogins, opt => opt.Ignore())
                .ForMember(x => x.Claims, opt => opt.Ignore());

            
            CreateMap<UserClaim, Claim>()
                .ConstructUsing(x => new Claim(x.Type, x.Type));

            CreateMap<UserLogin, Models.UserLogin>();
        }

    }
}
