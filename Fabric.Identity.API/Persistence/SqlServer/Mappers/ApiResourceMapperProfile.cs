using System.Linq;
using AutoMapper;
using Fabric.Identity.API.Persistence.SqlServer.EntityModels;

namespace Fabric.Identity.API.Persistence.SqlServer.Mappers
{
    public class ApiResourceMapperProfile : Profile
    {
        public ApiResourceMapperProfile()
        {
            // entity to model
            CreateMap<ApiResource, IdentityServer4.Models.ApiResource>(MemberList.Destination)
                .ConstructUsing(src => new IdentityServer4.Models.ApiResource())
                .ForMember(x => x.ApiSecrets, opt => opt.MapFrom(src => src.ApiSecrets.Select(x => x)))
                .ForMember(x => x.Scopes, opt => opt.MapFrom(src => src.ApiScopes.Select(x => x)))
                .ForMember(x => x.UserClaims, opts => opts.MapFrom(src => src.ApiClaims.Select(x => x.Type)));

            CreateMap<ApiSecret, IdentityServer4.Models.Secret>(MemberList.Destination);

            CreateMap<ApiScope, IdentityServer4.Models.Scope>(MemberList.Destination)
                .ForMember(x => x.UserClaims, opt => opt.MapFrom(src => src.ApiScopeClaims.Select(x => x.Type)));

            // model to entity
            CreateMap<IdentityServer4.Models.ApiResource, ApiResource>(MemberList.Source)
                .ForMember(x => x.ApiSecrets, opts => opts.MapFrom(src => src.ApiSecrets.Select(x => x)))
                .ForMember(x => x.ApiScopes, opts => opts.MapFrom(src => src.Scopes.Select(x => x)))
                .ForMember(x => x.ApiClaims, opts => opts.MapFrom(src => src.UserClaims.Select(x => new ApiClaim { Type = x })));

            CreateMap<IdentityServer4.Models.Secret, ApiSecret>(MemberList.Source);

            CreateMap<IdentityServer4.Models.Scope, ApiScope>(MemberList.Source)
                .ForMember(x => x.ApiScopeClaims, opts => opts.MapFrom(src => src.UserClaims.Select(x => new ApiScopeClaim { Type = x })));
        }
    }
}
