using System.Collections.Generic;

namespace Fabric.Identity.Client.Models
{
    using System.Security.Claims;

    /// <summary>
    /// The client.
    /// </summary>
    public class Client
    {
        // Basic

        /// <summary>
        /// Gets or sets the client id.
        /// </summary>
        public string ClientId { get; set; }

        /// <summary>
        /// Gets or sets the client secret.
        /// </summary>
        public string ClientSecret { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether require client secret.
        /// </summary>
        public bool RequireClientSecret { get; set; } = true;

        /// <summary>
        /// Gets or sets the allowed grant types.
        /// </summary>
        public IEnumerable<string> AllowedGrantTypes { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether require PKCE.
        /// </summary>
        public bool RequirePkce { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether allow plain text PKCE.
        /// </summary>
        public bool AllowPlainTextPkce { get; set; }

        /// <summary>
        /// Gets or sets the redirect uris.
        /// </summary>
        public ICollection<string> RedirectUris { get; set; } = new HashSet<string>();

        /// <summary>
        /// Gets or sets the allowed scopes.
        /// </summary>
        public ICollection<string> AllowedScopes { get; set; } = new HashSet<string>();

        /// <summary>
        /// Gets or sets a value indicating whether allow offline access.
        /// </summary>
        public bool AllowOfflineAccess { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether allow access tokens via browser.
        /// </summary>
        public bool AllowAccessTokensViaBrowser { get; set; }

        /// <summary>
        /// Gets the protocol type.
        /// </summary>
        public string ProtocolType { get; } = "oidc";

        // Authentication/Logout

        /// <summary>
        /// Gets or sets the post logout redirect uris.
        /// </summary>
        public ICollection<string> PostLogoutRedirectUris { get; set; } = new HashSet<string>();

        /// <summary>
        /// Gets a value indicating whether enable local login.
        /// </summary>
        public bool EnableLocalLogin { get; } = false;

        /// <summary>
        /// Gets or sets the logout uri.
        /// </summary>
        public string LogoutUri { get; set; }

        /// <summary>
        /// Gets a value indicating whether logout session required.
        /// </summary>
        public bool LogoutSessionRequired { get; } = true;

        // Token

        /// <summary>
        /// Gets or sets the identity token lifetime.
        /// </summary>
        public int IdentityTokenLifetime { get; set; } = 300;

        /// <summary>
        /// Gets or sets the access token lifetime.
        /// </summary>
        public int AccessTokenLifetime { get; set; } = 3600;

        /// <summary>
        /// Gets or sets the authorization code lifetime.
        /// </summary>
        public int AuthorizationCodeLifetime { get; set; } = 300;

        /// <summary>
        /// Gets or sets the absolute refresh token lifetime.
        /// </summary>
        public int AbsoluteRefreshTokenLifetime { get; set; } = 2592000;

        /// <summary>
        /// Gets or sets the sliding refresh token lifetime.
        /// </summary>
        public int SlidingRefreshTokenLifetime { get; set; } = 1296000;

        /// <summary>
        /// Gets or sets a value indicating whether update access token claims on refresh.
        /// </summary>
        public bool UpdateAccessTokenClaimsOnRefresh { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether include JWT id.
        /// </summary>
        public bool IncludeJwtId { get; set; }

        /// <summary>
        /// Gets or sets the allowed CORS origins.
        /// </summary>
        public ICollection<string> AllowedCorsOrigins { get; set; } = new HashSet<string>();

        /// <summary>
        /// Gets or sets the claims.
        /// </summary>
        public ICollection<Claim> Claims { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether always send client claims.
        /// </summary>
        public bool AlwaysSendClientClaims { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether always include user claims in id token.
        /// </summary>
        public bool AlwaysIncludeUserClaimsInIdToken { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether prefix client claims.
        /// </summary>
        public bool PrefixClientClaims { get; set; } = true;

        // Consent fields

        /// <summary>
        /// Gets or sets a value indicating whether require consent.
        /// </summary>
        public bool RequireConsent { get; set; } = false;

        /// <summary>
        /// Gets or sets a value indicating whether allow remember consent.
        /// </summary>
        public bool AllowRememberConsent { get; set; } = true;

        /// <summary>
        /// Gets or sets the client name.
        /// </summary>
        public string ClientName { get; set; }

        /// <summary>
        /// Gets or sets the client uri.
        /// </summary>
        public string ClientUri { get; set; }

        /// <summary>
        /// Gets or sets the logo uri.
        /// </summary>
        public string LogoUri { get; set; }
    }
}
