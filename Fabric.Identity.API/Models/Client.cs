using System.Collections.Generic;
using System.Security.Claims;
using IdentityModel;
using IdentityServer4;
using IdentityServer4.Models;

namespace Fabric.Identity.API.Models
{
    /// <summary>
    /// http://docs.identityserver.io/en/release/reference/client.html
    /// </summary>
    public class Client
    {
        // Basic
        public bool Enabled { get; set; } = true;
        public string ClientId { get; set; }
        public string ClientSecret { get; set; }
        public bool RequireClientSecret { get; set; } = true;
        public IEnumerable<string> AllowedGrantTypes { get; set; } = GrantTypes.Implicit;
        public bool RequirePkce { get; set; } = false;
        public bool AllowPlainTextPkce { get; set; } = false;
        public ICollection<string> RedirectUris { get; set; } = new HashSet<string>();
        public ICollection<string> AllowedScopes { get; set; } = new HashSet<string>();
        public bool AllowOfflineAccess { get; set; } = false;
        public bool AllowAccessTokensViaBrowser { get; set; } = false;
        public string ProtocolType { get; set; } = IdentityServerConstants.ProtocolTypes.OpenIdConnect;

        // Authentication/Logout
        public ICollection<string> PostLogoutRedirectUris { get; set; } = new HashSet<string>();
        public bool EnableLocalLogin { get; set; } = true;
        public ICollection<string> IdentityProviderRestrictions { get; set; } = new HashSet<string>();
        public string LogoutUri { get; set; }
        public bool LogoutSessionRequired { get; set; } = true;

        // Token
        public int IdentityTokenLifetime { get; set; } = 300;
        public int AccessTokenLifetime { get; set; } = 3600;
        public int AuthorizationCodeLifetime { get; set; } = 300;
        public int AbsoluteRefreshTokenLifetime { get; set; } = 2592000;
        public int SlidingRefreshTokenLifetime { get; set; } = 1296000;
        public TokenUsage RefreshTokenUsage { get; set; } = TokenUsage.OneTimeOnly;
        public TokenExpiration RefreshTokenExpiration { get; set; } = TokenExpiration.Absolute;
        public bool UpdateAccessTokenClaimsOnRefresh { get; set; } = false;
        public AccessTokenType AccessTokenType { get; set; } = AccessTokenType.Jwt;
        public bool IncludeJwtId { get; set; } = false;
        public ICollection<string> AllowedCorsOrigins { get; set; } = new HashSet<string>();
        public ICollection<Claim> Claims { get; set; } = new HashSet<Claim>(new ClaimComparer());
        public bool AlwaysSendClientClaims { get; set; } = false;
        public bool AlwaysIncludeUserClaimsInIdToken { get; set; } = false;
        public bool PrefixClientClaims { get; set; } = true;

        // Consent fields
        public bool RequireConsent { get; set; } = true;
        public bool AllowRememberConsent { get; set; } = true;
        public string ClientName { get; set; }
        public string ClientUri { get; set; }
        public string LogoUri { get; set; }
    }
}