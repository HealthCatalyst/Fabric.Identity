using System.Linq;
using AutoMapper;
using Fabric.Identity.API.Persistence.SqlServer.EntityModels;
using System.Security.Claims;


namespace Fabric.Identity.API.Persistence.SqlServer.Mappers
{
    public class ClientMapperProfile : Profile
    {
        public ClientMapperProfile()
        {
            // entity to model
            CreateMap<Client, IdentityServer4.Models.Client>(MemberList.Destination)               
                .ForMember(x => x.AllowedGrantTypes,
                    opt => opt.MapFrom(src => src.ClientGrantTypes.Select(x => x.GrantType)))
                .ForMember(x => x.RedirectUris, opt => opt.MapFrom(src => src.ClientRedirectUris.Select(x => x.RedirectUri)))
                .ForMember(x => x.PostLogoutRedirectUris,
                    opt => opt.MapFrom(src => src.ClientPostLogoutRedirectUris.Select(x => x.PostLogoutRedirectUri)))
                .ForMember(x => x.AllowedScopes, opt => opt.MapFrom(src => src.ClientScopes.Select(x => x.Scope)))
                .ForMember(x => x.ClientSecrets, opt => opt.MapFrom(src => src.ClientSecrets.Select(x => x)))
                .ForMember(x => x.Claims, opt => opt.MapFrom(src => src.ClientClaims.Select(x => new Claim(x.Type, x.Value))))
                .ForMember(x => x.IdentityProviderRestrictions,
                    opt => opt.MapFrom(src => src.ClientIdpRestrictions.Select(x => x.Provider)))
                .ForMember(x => x.AllowedCorsOrigins,
                    opt => opt.MapFrom(src => src.ClientCorsOrigins.Select(x => x.Origin)));

            CreateMap<ClientSecret, IdentityServer4.Models.Secret>(MemberList.Destination)
                .ForMember(dest => dest.Type, opt => opt.Condition(srs => srs != null));

            // model to entity
            CreateMap< IdentityServer4.Models.Client, Client>(MemberList.Source)                
                .ForMember(x => x.ClientGrantTypes,
                    opt => opt.MapFrom(src => src.AllowedGrantTypes.Select(x => new ClientGrantType { GrantType = x })))
                .ForMember(x => x.ClientRedirectUris,
                    opt => opt.MapFrom(src => src.RedirectUris.Select(x => new ClientRedirectUri { RedirectUri = x })))
                .ForMember(x => x.ClientPostLogoutRedirectUris,
                    opt =>
                        opt.MapFrom(
                            src =>
                                src.PostLogoutRedirectUris.Select(
                                    x => new ClientPostLogoutRedirectUri { PostLogoutRedirectUri = x })))
                .ForMember(x => x.ClientScopes,
                    opt => opt.MapFrom(src => src.AllowedScopes.Select(x => new ClientScope { Scope = x })))
                .ForMember(x => x.ClientClaims,
                    opt => opt.MapFrom(src => src.Claims.Select(x => new ClientClaim { Type = x.Type, Value = x.Value })))
                .ForMember(x => x.ClientIdpRestrictions,
                    opt =>
                        opt.MapFrom(
                            src => src.IdentityProviderRestrictions.Select(x => new ClientIdpRestriction { Provider = x })))
                .ForMember(x => x.ClientCorsOrigins,
                    opt => opt.MapFrom(src => src.AllowedCorsOrigins.Select(x => new ClientCorsOrigin { Origin = x })));

            CreateMap<IdentityServer4.Models.Secret, ClientSecret>(MemberList.Source);

        }
    }
}
