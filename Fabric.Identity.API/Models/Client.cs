using System.Collections.Generic;
using System.Security.Claims;
using IdentityServer4.Models;

namespace Fabric.Identity.API.Models
{
    /// <summary>
    /// http://docs.identityserver.io/en/release/reference/client.html
    /// </summary>
    public class Client
    {
        // Basic
        public bool Enabled { get; set; }
        public string ClientId { get; set; }
        public string ClientSecret { get; set; }
        public bool RequireClientSecret { get; set; }
        public IEnumerable<string> AllowedGrantTypes { get; set; }
        public bool RequirePkce { get; set; }
        public bool AllowPlainTextPkce { get; set; }
        public ICollection<string> RedirectUris { get; set; }
        public ICollection<string> AllowedScopes { get; set; }
        public bool AllowOfflineAccess { get; set; }
        public bool AllowAccessTokensViaBrowser { get; set; }
        public string ProtocolType { get; set; }

        // Authentication/Logout
        public ICollection<string> PostLogoutRedirectUris { get; set; }
        public bool EnableLocalLogin { get; set; }
        public ICollection<string> IdentityProviderRestrictions { get; set; }
        public string LogoutUri { get; set; }
        public bool LogoutSessionRequired { get; set; }

        // Token
        public int IdentityTokenLifetime { get; set; }
        public int AccessTokenLifetime { get; set; }
        public int AuthorizationCodeLifetime { get; set; }
        public int AbsoluteRefreshTokenLifetime { get; set; }
        public int SlidingRefreshTokenLifetime { get; set; }
        public TokenUsage RefreshTokenUsage { get; set; }
        public TokenExpiration RefreshTokenExpiration { get; set; }
        public bool UpdateAccessTokenClaimsOnRefresh { get; set; }
        public AccessTokenType AccessTokenType { get; set; }
        public bool IncludeJwtId { get; set; }
        public ICollection<string> AllowedCorsOrigins { get; set; }
        public ICollection<Claim> Claims { get; set; }
        public bool AlwaysSendClientClaims { get; set; }
        public bool AlwaysIncludeUserClaimsInIdToken { get; set; }
        public bool PrefixClientClaims { get; set; }

        // Consent fields
        public bool RequireConsent { get; set; }
        public bool AllowRememberConsent { get; set; }
        public string ClientName { get; set; }
        public string ClientUri { get; set; }
        public string LogoUri { get; set; }
    }
}