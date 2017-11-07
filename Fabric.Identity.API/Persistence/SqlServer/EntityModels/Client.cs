using System;
using System.Collections.Generic;
using Fabric.Identity.API.Persistence.SqlServer.Entities;
using IdentityServer4;
using IdentityServer4.Models;

namespace Fabric.Identity.API.Persistence.SqlServer.EntityModels
{
    public partial class Client : ITrackable, ISoftDelete
    {
        public Client()
        {
            ClientClaims = new HashSet<ClientClaim>();
            ClientCorsOrigins = new HashSet<ClientCorsOrigin>();
            ClientGrantTypes = new HashSet<ClientGrantType>();
            ClientIdPrestrictions = new HashSet<ClientIdPrestriction>();
            ClientPostLogoutRedirectUris = new HashSet<ClientPostLogoutRedirectUri>();
            ClientRedirectUris = new HashSet<ClientRedirectUri>();
            ClientScopes = new HashSet<ClientScope>();
            ClientSecrets = new HashSet<ClientSecret>();
            UserLogins = new HashSet<UserLogin>();
        }

        public int Id { get; set; }
        public bool Enabled { get; set; } = true;
        public int AbsoluteRefreshTokenLifetime { get; set; } = 2592000;
        public int AccessTokenLifetime { get; set; } = 3600;
        public int AccessTokenType { get; set; } = 0; 
        public bool AllowAccessTokensViaBrowser { get; set; }
        public bool AllowOfflineAccess { get; set; }
        public bool AllowPlainTextPkce { get; set; }
        public bool AllowRememberConsent { get; set; } = true;
        public bool AlwaysIncludeUserClaimsInIdToken { get; set; }
        public bool AlwaysSendClientClaims { get; set; }
        public int AuthorizationCodeLifetime { get; set; } = 300;
        public bool BackChannelLogoutSessionRequired { get; set; } = true;
        public string ClientId { get; set; }
        public string ClientName { get; set; }
        public string ClientUri { get; set; }
        public int? ConsentLifetime { get; set; } = null;
        public bool EnableLocalLogin { get; set; } = true;
        public bool FrontChannelLogoutSessionRequired { get; set; } = true;
        public int IdentityTokenLifetime { get; set; } = 300;
        public bool IncludeJwtId { get; set; }
        public string LogoUri { get; set; }
        public string ProtocolType { get; set; } = IdentityServerConstants.ProtocolTypes.OpenIdConnect;
        public int RefreshTokenExpiration { get; set; } = (int)TokenExpiration.Absolute;
        public int RefreshTokenUsage { get; set; } = (int)TokenUsage.OneTimeOnly;
        public bool RequireClientSecret { get; set; } = true;
        public bool RequireConsent { get; set; } = true;
        public bool RequirePkce { get; set; }
        public int SlidingRefreshTokenLifetime { get; set; } = 1296000;
        public bool UpdateAccessTokenClaimsOnRefresh { get; set; }
        public DateTime CreatedDateTimeUtc { get; set; }
        public DateTime? ModifiedDateTimeUtc { get; set; }
        public string CreatedBy { get; set; }
        public string ModifiedBy { get; set; }
        public bool IsDeleted { get; set; }

        public virtual ICollection<ClientClaim> ClientClaims { get; set; }
        public virtual ICollection<ClientCorsOrigin> ClientCorsOrigins { get; set; }
        public virtual ICollection<ClientGrantType> ClientGrantTypes { get; set; }
        public virtual ICollection<ClientIdPrestriction> ClientIdPrestrictions { get; set; }
        public virtual ICollection<ClientPostLogoutRedirectUri> ClientPostLogoutRedirectUris { get; set; }
        public virtual ICollection<ClientRedirectUri> ClientRedirectUris { get; set; }
        public virtual ICollection<ClientScope> ClientScopes { get; set; }
        public virtual ICollection<ClientSecret> ClientSecrets { get; set; }
        public virtual ICollection<UserLogin> UserLogins { get; set; }
    }
}
