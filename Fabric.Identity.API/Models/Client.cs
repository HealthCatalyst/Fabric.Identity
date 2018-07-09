using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
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
        [DefaultValue(true)]
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// Unique client ID. Required in the request body ONLY for POST operations.
        /// </summary>
        [Required]
        public string ClientId { get; set; }

        public string ClientSecret { get; set; }

        [DefaultValue(true)]
        public bool RequireClientSecret { get; set; } = true;

        /// <summary>
        /// Supported grant types for this client.
        /// Valid options include the following:
        ///     implicit | hybrid | authorization_code | client_credentials | password | delegation | delegation, client_credentials
        /// </summary>
        [Required]
        public IEnumerable<string> AllowedGrantTypes { get; set; } = GrantTypes.Implicit;

        /// <summary>
        /// Set to true for Hybrid+PKCE clients.
        /// </summary>
        public bool RequirePkce { get; set; }

        public bool AllowPlainTextPkce { get; set; }

        public ICollection<string> RedirectUris { get; set; } = new HashSet<string>();

        [Required]
        public ICollection<string> AllowedScopes { get; set; } = new HashSet<string>();

        /// <summary>
        /// Indicates whether the client can request refresh tokens (not allowed for implicit or password grant types).
        /// </summary>
        [Required]
        public bool AllowOfflineAccess { get; set; }

        public bool AllowAccessTokensViaBrowser { get; set; }

        [DefaultValue(IdentityServerConstants.ProtocolTypes.OpenIdConnect)]
        public string ProtocolType { get; set; } = IdentityServerConstants.ProtocolTypes.OpenIdConnect;

        // Authentication/Logout
        public ICollection<string> PostLogoutRedirectUris { get; set; } = new HashSet<string>();

        [DefaultValue(true)]
        public bool EnableLocalLogin { get; set; } = true;

        public ICollection<string> IdentityProviderRestrictions { get; set; } = new HashSet<string>();
        public string LogoutUri { get; set; }

        [DefaultValue(true)]
        public bool LogoutSessionRequired { get; set; } = true;

        // Token
        [DefaultValue(300)]
        public int IdentityTokenLifetime { get; set; } = 300;

        [DefaultValue(3600)]
        public int AccessTokenLifetime { get; set; } = 3600;

        [DefaultValue(300)]
        public int AuthorizationCodeLifetime { get; set; } = 300;

        [DefaultValue(259200)]
        public int AbsoluteRefreshTokenLifetime { get; set; } = 2592000;

        [DefaultValue(1296000)]
        public int SlidingRefreshTokenLifetime { get; set; } = 1296000;

        [DefaultValue("OneTimeOnly")]
        public TokenUsage RefreshTokenUsage { get; set; } = TokenUsage.OneTimeOnly;

        [DefaultValue("Absolute")]
        public TokenExpiration RefreshTokenExpiration { get; set; } = TokenExpiration.Absolute;
        public bool UpdateAccessTokenClaimsOnRefresh { get; set; }

        [DefaultValue("Jwt")]
        public AccessTokenType AccessTokenType { get; set; } = AccessTokenType.Jwt;

        public bool IncludeJwtId { get; set; }
        public ICollection<string> AllowedCorsOrigins { get; set; } = new HashSet<string>();
        public ICollection<Claim> Claims { get; set; } = new HashSet<Claim>(new ClaimComparer());
        public bool AlwaysSendClientClaims { get; set; }
        public bool AlwaysIncludeUserClaimsInIdToken { get; set; }
        [DefaultValue(true)]
        public bool PrefixClientClaims { get; set; } = true;

        // Consent fields
        [DefaultValue(true)]
        public bool RequireConsent { get; set; } = true;
        [DefaultValue(true)]
        public bool AllowRememberConsent { get; set; } = true;

        [Required]
        public string ClientName { get; set; }
        public string ClientUri { get; set; }
        public string LogoUri { get; set; }
    }
}