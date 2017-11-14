CREATE TABLE [dbo].[Clients]
(
	[Id] int NOT NULL IDENTITY,
    [AbsoluteRefreshTokenLifetime] int NOT NULL,
    [AccessTokenLifetime] int NOT NULL,
    [AccessTokenType] int NOT NULL,
    [AllowAccessTokensViaBrowser] bit NOT NULL,
    [AllowOfflineAccess] bit NOT NULL,
    [AllowPlainTextPkce] bit NOT NULL,
    [AllowRememberConsent] bit NOT NULL,
    [AlwaysIncludeUserClaimsInIdToken] bit NOT NULL,
    [AlwaysSendClientClaims] bit NOT NULL,
    [AuthorizationCodeLifetime] int NOT NULL,
    [BackChannelLogoutSessionRequired] bit NOT NULL,        
    [ClientId] nvarchar(200) NOT NULL,
    [ClientName] nvarchar(200) NULL,
    [ClientUri] nvarchar(2000) NULL,
    [ConsentLifetime] int NULL,    
    [EnableLocalLogin] bit NOT NULL,
    [Enabled] bit NOT NULL,
    [FrontChannelLogoutSessionRequired] bit NOT NULL,    
    [IdentityTokenLifetime] int NOT NULL,
    [IncludeJwtId] bit NOT NULL,
    [LogoUri] nvarchar(2000) NULL,    
    [ProtocolType] nvarchar(200) NOT NULL,
    [RefreshTokenExpiration] int NOT NULL,
    [RefreshTokenUsage] int NOT NULL,
    [RequireClientSecret] bit NOT NULL,
    [RequireConsent] bit NOT NULL,
    [RequirePkce] bit NOT NULL,
    [SlidingRefreshTokenLifetime] int NOT NULL,
    [UpdateAccessTokenClaimsOnRefresh] bit NOT NULL,
	[CreatedDateTimeUtc] datetime NOT NULL,
	[ModifiedDateTimeUtc] datetime	NULL,
	[CreatedBy] nvarchar(100) NOT NULL,
	[ModifiedBy] nvarchar(100) NULL,
	[IsDeleted] bit default 0 NOT NULL,
    CONSTRAINT [PK_Clients] PRIMARY KEY ([Id])
);

GO

CREATE UNIQUE INDEX [IX_Clients_ClientId] ON [Clients] ([ClientId]);

GO