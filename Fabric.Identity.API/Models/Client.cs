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

        /// <summary>
        /// Specifies if client is enabled (defaults to true).
        /// </summary>
        [DefaultValue(true)]
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// Unique client ID. Required in the request body ONLY for POST operations.
        /// </summary>
        [Required]
        public string ClientId { get; set; }

        /// <summary>
        /// Client secrets - only relevant for flows that require a secret.
        /// </summary>
        public string ClientSecret { get; set; }

        /// <summary>
        /// If set to false, no client secret is needed to request tokens at the token endpoint (defaults to true).
        /// </summary>
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
        ///  Specifies whether a proof key is required for authorization code based token requests.
        /// </summary>
        public bool RequirePkce { get; set; }

        /// <summary>
        /// Specifies whether a proof key can be sent using plain method (not recommended 
        /// and default to false).
        /// </summary>
        public bool AllowPlainTextPkce { get; set; }

        /// <summary>
        /// Specifies allowed URIs to return tokens or authorization codes to.
        /// </summary>
        public ICollection<string> RedirectUris { get; set; } = new HashSet<string>();

        /// <summary>
        /// Specifies the API scopes that the client is allowed to request. If empty, the 
        /// client can't access any scope.
        /// </summary>
        [Required]
        public ICollection<string> AllowedScopes { get; set; } = new HashSet<string>();

        /// <summary>
        /// Indicates whether the client can request refresh tokens (not allowed for implicit or password grant types).
        /// </summary>
        [Required]
        public bool AllowOfflineAccess { get; set; }

        /// <summary>
        /// Controls whether access tokens are transmitted via the browser for this client 
        /// (defaults to false). This can prevent accidental leakage of access tokens when 
        /// multiple response types are allowed.
        /// </summary>
        public bool AllowAccessTokensViaBrowser { get; set; }

        /// <summary>
        /// Gets or sets the protocol type. Defaults to "oidc".
        /// </summary>
        [DefaultValue(IdentityServerConstants.ProtocolTypes.OpenIdConnect)]
        public string ProtocolType { get; set; } = IdentityServerConstants.ProtocolTypes.OpenIdConnect;

        // Authentication/Logout

        /// <summary>
        ///  Specifies allowed URIs to redirect to after logout.
        /// </summary>
        public ICollection<string> PostLogoutRedirectUris { get; set; } = new HashSet<string>();

        /// <summary>
        /// Gets or sets a value indicating whether the local login is allowed for this client. 
        /// Defaults to true.
        /// </summary>
        [DefaultValue(true)]
        public bool EnableLocalLogin { get; set; } = true;

        /// <summary>
        /// Specifies which external IdPs can be used with this client (if list is 
        /// empty all IdPs are allowed). Defaults to empty.
        /// </summary>
        public ICollection<string> IdentityProviderRestrictions { get; set; } = new HashSet<string>();

        /// <summary>
        /// Specifies logout URI at client for HTTP based logout.
        /// </summary>
        public string LogoutUri { get; set; }

        /// <summary>
        /// Specifies is the user's session id should be sent to the LogoutUri. 
        /// Defaults to true.
        /// </summary>
        [DefaultValue(true)]
        public bool LogoutSessionRequired { get; set; } = true;

        // Token

        /// <summary>
        /// Lifetime of identity token in seconds (defaults to 300 seconds / 5 minutes).
        /// </summary>
        [DefaultValue(300)]
        public int IdentityTokenLifetime { get; set; } = 300;

        /// <summary>
        /// Lifetime of access token in seconds (defaults to 3600 seconds / 1 hour).
        /// </summary>
        [DefaultValue(3600)]
        public int AccessTokenLifetime { get; set; } = 3600;

        /// <summary>
        /// Lifetime of authorization code in seconds (defaults to 300 seconds / 5 minutes).
        /// </summary>
        [DefaultValue(300)]
        public int AuthorizationCodeLifetime { get; set; } = 300;

        /// <summary>
        /// Maximum lifetime of a refresh token in seconds. Defaults to 2592000 seconds / 30 days.
        /// </summary>
        [DefaultValue(259200)]
        public int AbsoluteRefreshTokenLifetime { get; set; } = 2592000;

        /// <summary>
        ///  Sliding lifetime of a refresh token in seconds. Defaults to 1296000 seconds / 15 days.
        /// </summary>
        [DefaultValue(1296000)]
        public int SlidingRefreshTokenLifetime { get; set; } = 1296000;

        /// <summary>
        /// ReUse: the refresh token handle will stay the same when refreshing tokens 
        /// OneTime: the refresh token handle will be updated when refreshing tokens
        /// </summary>
        [DefaultValue("OneTimeOnly")]
        public TokenUsage RefreshTokenUsage { get; set; } = TokenUsage.OneTimeOnly;

        /// <summary>
        /// Absolute: the refresh token will expire on a fixed point in time (specified by 
        /// the AbsoluteRefreshTokenLifetime) Sliding: when refreshing the token, the lifetime of 
        /// the refresh token will be renewed (by the amount specified in SlidingRefreshTokenLifetime). 
        /// The lifetime will not exceed AbsoluteRefreshTokenLifetime.
        /// </summary>
        [DefaultValue("Absolute")]
        public TokenExpiration RefreshTokenExpiration { get; set; } = TokenExpiration.Absolute;

        /// <summary>
        /// Gets or sets a value indicating whether the access token (and its claims) 
        /// should be updated on a refresh token request.
        /// </summary>
        public bool UpdateAccessTokenClaimsOnRefresh { get; set; }

        /// <summary>
        /// Specifies whether the access token is a reference token or a self contained JWT 
        /// token (defaults to Jwt).
        /// </summary>
        [DefaultValue("Jwt")]
        public AccessTokenType AccessTokenType { get; set; } = AccessTokenType.Jwt;

        /// <summary>
        ///  Gets or sets a value indicating whether JWT access tokens should include an identifier.
        /// </summary>
        public bool IncludeJwtId { get; set; }

        /// <summary>
        /// Gets or sets the allowed CORS origins for JavaScript clients.
        /// </summary>
        public ICollection<string> AllowedCorsOrigins { get; set; } = new HashSet<string>();

        /// <summary>
        /// Allows settings claims for the client (will be included in the access token).
        /// </summary>
        public ICollection<Claim> Claims { get; set; } = new HashSet<Claim>(new ClaimComparer());

        /// <summary>
        ///  Gets or sets a value indicating whether client claims should be always included 
        /// in the access tokens - only for client credentials flow.
        /// </summary>
        public bool AlwaysSendClientClaims { get; set; }

        /// <summary>
        /// When requesting both an id token and access token, should the user claims always be 
        /// added to the id token instead of requring the client to use the userinfo endpoint.
        /// </summary>
        public bool AlwaysIncludeUserClaimsInIdToken { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether all client claims should be prefixed.
        /// </summary>
        [DefaultValue(true)]
        public bool PrefixClientClaims { get; set; } = true;

        // Consent fields

        /// <summary>
        /// Specifies whether a consent screen is required (defaults to true).
        /// </summary>
        [DefaultValue(true)]
        public bool RequireConsent { get; set; } = true;

        /// <summary>
        /// Specifies whether user can choose to store consent decisions (defaults to true).
        /// </summary>
        [DefaultValue(true)]
        public bool AllowRememberConsent { get; set; } = true;

        /// <summary>
        /// Client display name (used for logging and consent screen).
        /// </summary>
        [Required]
        public string ClientName { get; set; }

        /// <summary>
        /// URI for further information about client (used on consent screen).
        /// </summary>
        public string ClientUri { get; set; }

        /// <summary>
        /// URI to client logo (used on consent screen).
        /// </summary>
        public string LogoUri { get; set; }
    }
}