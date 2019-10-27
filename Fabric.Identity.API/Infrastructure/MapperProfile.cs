using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Fabric.Identity.API.Models;

namespace Fabric.Identity.API.Infrastructure
{
    public class MapperProfile : Profile
    {
        public MapperProfile()
        {
            CreateMap<FabricGroup, FabricGroupApiModel>();

            CreateMap<FabricPrincipal, FabricPrincipalApiModel>()
                .ForMember(x => x.PrincipalType, 
                    opt => opt.MapFrom(p => p.PrincipalType.ToString().ToLower()));

            CreateMap<User, FabricPrincipal>()
                .ForMember(p => p.IdentityProvider, opt => opt.MapFrom(u => u.ProviderName))
                .ForMember(p => p.Email, opt => opt.MapFrom(u => u.Username))
                .ForMember(p => p.DisplayName, opt => opt.MapFrom(u => u.Username));
        }
    }
}
