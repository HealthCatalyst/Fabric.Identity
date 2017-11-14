USE [Identity]

CREATE TABLE [ApiResources] (
    [Id] int NOT NULL IDENTITY,
    [Description] nvarchar(1000) NULL,
    [DisplayName] nvarchar(200) NULL,
    [Enabled] bit NOT NULL,
    [Name] nvarchar(200) NOT NULL,
	[CreatedDateTimeUtc] datetime NOT NULL,
	[ModifiedDateTimeUtc] datetime	NULL,
	[CreatedBy] nvarchar(100) NOT NULL,
	[ModifiedBy] nvarchar(100) NULL,
	[IsDeleted] bit default 0 NOT NULL,
    CONSTRAINT [PK_ApiResources] PRIMARY KEY ([Id])
);

GO

CREATE TABLE [Clients] (
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

CREATE TABLE [IdentityResources] (
    [Id] int NOT NULL IDENTITY,
    [Description] nvarchar(1000) NULL,
    [DisplayName] nvarchar(200) NULL,
    [Emphasize] bit NOT NULL,
    [Enabled] bit NOT NULL,
    [Name] nvarchar(200) NOT NULL,
    [Required] bit NOT NULL,
    [ShowInDiscoveryDocument] bit NOT NULL,
	[CreatedDateTimeUtc] datetime NOT NULL,
	[ModifiedDateTimeUtc] datetime	NULL,
	[CreatedBy] nvarchar(100) NOT NULL,
	[ModifiedBy] nvarchar(100) NULL,
	[IsDeleted] bit default 0 NOT NULL,
    CONSTRAINT [PK_IdentityResources] PRIMARY KEY ([Id])
);

GO

CREATE TABLE [ApiClaims] (
    [Id] int NOT NULL IDENTITY,
    [ApiResourceId] int NOT NULL,
    [Type] nvarchar(200) NOT NULL,
    CONSTRAINT [PK_ApiClaims] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_ApiClaims_ApiResources_ApiResourceId] FOREIGN KEY ([ApiResourceId]) REFERENCES [ApiResources] ([Id]) ON DELETE CASCADE
);

GO

CREATE TABLE [ApiScopes] (
    [Id] int NOT NULL IDENTITY,
    [ApiResourceId] int NOT NULL,
    [Description] nvarchar(1000) NULL,
    [DisplayName] nvarchar(200) NULL,
    [Emphasize] bit NOT NULL,
    [Name] nvarchar(200) NOT NULL,
    [Required] bit NOT NULL,
    [ShowInDiscoveryDocument] bit NOT NULL,
    CONSTRAINT [PK_ApiScopes] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_ApiScopes_ApiResources_ApiResourceId] FOREIGN KEY ([ApiResourceId]) REFERENCES [ApiResources] ([Id]) ON DELETE CASCADE
);

GO

CREATE TABLE [ApiSecrets] (
    [Id] int NOT NULL IDENTITY,
    [ApiResourceId] int NOT NULL,
    [Description] nvarchar(1000) NULL,
    [Expiration] datetime2 NULL,
    [Type] nvarchar(250) NULL,
    [Value] nvarchar(2000) NULL,
    CONSTRAINT [PK_ApiSecrets] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_ApiSecrets_ApiResources_ApiResourceId] FOREIGN KEY ([ApiResourceId]) REFERENCES [ApiResources] ([Id]) ON DELETE CASCADE
);

GO

CREATE TABLE [ClientClaims] (
    [Id] int NOT NULL IDENTITY,
    [ClientId] int NOT NULL,
    [Type] nvarchar(250) NOT NULL,
    [Value] nvarchar(250) NOT NULL,
    CONSTRAINT [PK_ClientClaims] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_ClientClaims_Clients_ClientId] FOREIGN KEY ([ClientId]) REFERENCES [Clients] ([Id]) ON DELETE CASCADE
);

GO

CREATE TABLE [ClientCorsOrigins] (
    [Id] int NOT NULL IDENTITY,
    [ClientId] int NOT NULL,
    [Origin] nvarchar(150) NOT NULL,
    CONSTRAINT [PK_ClientCorsOrigins] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_ClientCorsOrigins_Clients_ClientId] FOREIGN KEY ([ClientId]) REFERENCES [Clients] ([Id]) ON DELETE CASCADE
);

GO

CREATE TABLE [ClientGrantTypes] (
    [Id] int NOT NULL IDENTITY,
    [ClientId] int NOT NULL,
    [GrantType] nvarchar(250) NOT NULL,
    CONSTRAINT [PK_ClientGrantTypes] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_ClientGrantTypes_Clients_ClientId] FOREIGN KEY ([ClientId]) REFERENCES [Clients] ([Id]) ON DELETE CASCADE
);

GO

CREATE TABLE [ClientIdPRestrictions] (
    [Id] int NOT NULL IDENTITY,
    [ClientId] int NOT NULL,
    [Provider] nvarchar(200) NOT NULL,
    CONSTRAINT [PK_ClientIdPRestrictions] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_ClientIdPRestrictions_Clients_ClientId] FOREIGN KEY ([ClientId]) REFERENCES [Clients] ([Id]) ON DELETE CASCADE
);

GO

CREATE TABLE [ClientPostLogoutRedirectUris] (
    [Id] int NOT NULL IDENTITY,
    [ClientId] int NOT NULL,
    [PostLogoutRedirectUri] nvarchar(2000) NOT NULL,
    CONSTRAINT [PK_ClientPostLogoutRedirectUris] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_ClientPostLogoutRedirectUris_Clients_ClientId] FOREIGN KEY ([ClientId]) REFERENCES [Clients] ([Id]) ON DELETE CASCADE
);

GO

CREATE TABLE [ClientRedirectUris] (
    [Id] int NOT NULL IDENTITY,
    [ClientId] int NOT NULL,
    [RedirectUri] nvarchar(2000) NOT NULL,
    CONSTRAINT [PK_ClientRedirectUris] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_ClientRedirectUris_Clients_ClientId] FOREIGN KEY ([ClientId]) REFERENCES [Clients] ([Id]) ON DELETE CASCADE
);

GO

CREATE TABLE [ClientScopes] (
    [Id] int NOT NULL IDENTITY,
    [ClientId] int NOT NULL,
    [Scope] nvarchar(200) NOT NULL,
    CONSTRAINT [PK_ClientScopes] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_ClientScopes_Clients_ClientId] FOREIGN KEY ([ClientId]) REFERENCES [Clients] ([Id]) ON DELETE CASCADE
);

GO

CREATE TABLE [ClientSecrets] (
    [Id] int NOT NULL IDENTITY,
    [ClientId] int NOT NULL,
    [Description] nvarchar(2000) NULL,
    [Expiration] datetime2 NULL,
    [Type] nvarchar(250) NULL,
    [Value] nvarchar(2000) NOT NULL,
    CONSTRAINT [PK_ClientSecrets] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_ClientSecrets_Clients_ClientId] FOREIGN KEY ([ClientId]) REFERENCES [Clients] ([Id]) ON DELETE CASCADE
);

GO

CREATE TABLE [IdentityClaims] (
    [Id] int NOT NULL IDENTITY,
    [IdentityResourceId] int NOT NULL,
    [Type] nvarchar(200) NOT NULL,
    CONSTRAINT [PK_IdentityClaims] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_IdentityClaims_IdentityResources_IdentityResourceId] FOREIGN KEY ([IdentityResourceId]) REFERENCES [IdentityResources] ([Id]) ON DELETE CASCADE
);

GO

CREATE TABLE [ApiScopeClaims] (
    [Id] int NOT NULL IDENTITY,
    [ApiScopeId] int NOT NULL,
    [Type] nvarchar(200) NOT NULL,
    CONSTRAINT [PK_ApiScopeClaims] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_ApiScopeClaims_ApiScopes_ApiScopeId] FOREIGN KEY ([ApiScopeId]) REFERENCES [ApiScopes] ([Id]) ON DELETE CASCADE
);

GO

CREATE TABLE [Users](
	[Id] int NOT NULL IDENTITY,
	[SubjectId] nvarchar(200) NOT NULL,
	[ProviderName] nvarchar(200) NOT NULL,
	[FirstName] nvarchar(200) NULL,
	[MiddleName] nvarchar(200) NULL,
	[LastName] nvarchar(200) NULL,
	[Username] nvarchar(200) NOT NULL,
	[CreatedDateTimeUtc] datetime NOT NULL,
	[ModifiedDateTimeUtc] datetime	NULL,
	[CreatedBy] nvarchar(100) NOT NULL,
	[ModifiedBy] nvarchar(100) NULL,
	[ComputedUserId] AS SubjectId + ':' + ProviderName,
	CONSTRAINT [PK_[Users] PRIMARY KEY ([Id]),
);

GO

CREATE TABLE [UserClaims](
	[Id] int NOT NULL IDENTITY,
	[UserId] int NOT NULL,
	[Type] nvarchar(200) NOT NULL,
	CONSTRAINT [PK_UserClaims] PRIMARY KEY ([Id]),
	CONSTRAINT [FK_UserClaims_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id])
);

GO

CREATE TABLE [UserLogins](
	[Id] int NOT NULL IDENTITY,
	[UserId] int NOT NULL,
	[ClientId] nvarchar(200) NOT NULL,
	[LoginDate] datetime NOT NULL,	
	CONSTRAINT [PK_[UserLogins] PRIMARY KEY ([Id]),	
	CONSTRAINT [FK_UserLogins_Users_Id] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id])
);

GO

CREATE TABLE [PersistedGrants] (
    [Key] nvarchar(200) NOT NULL,
    [ClientId] nvarchar(200) NOT NULL,
    [CreationTime] datetime2 NOT NULL,
    [Data] nvarchar(max) NOT NULL,
    [Expiration] datetime2 NULL,
    [SubjectId] nvarchar(200) NULL,
    [Type] nvarchar(50) NOT NULL,
	[CreatedDateTimeUtc] datetime NOT NULL,
	[ModifiedDateTimeUtc] datetime	NULL,
	[CreatedBy] nvarchar(100) NOT NULL,
	[ModifiedBy] nvarchar(100) NULL,
	[IsDeleted] bit default 0 NOT NULL,
    CONSTRAINT [PK_PersistedGrants] PRIMARY KEY ([Key])
);

GO

CREATE INDEX [IX_ApiClaims_ApiResourceId] ON [ApiClaims] ([ApiResourceId]);

GO

CREATE UNIQUE INDEX [IX_ApiResources_Name] ON [ApiResources] ([Name]);

GO

CREATE INDEX [IX_ApiScopeClaims_ApiScopeId] ON [ApiScopeClaims] ([ApiScopeId]);

GO

CREATE INDEX [IX_ApiScopes_ApiResourceId] ON [ApiScopes] ([ApiResourceId]);

GO

CREATE UNIQUE INDEX [IX_ApiScopes_Name] ON [ApiScopes] ([Name]);

GO

CREATE INDEX [IX_ApiSecrets_ApiResourceId] ON [ApiSecrets] ([ApiResourceId]);

GO

CREATE INDEX [IX_ClientClaims_ClientId] ON [ClientClaims] ([ClientId]);

GO

CREATE INDEX [IX_ClientCorsOrigins_ClientId] ON [ClientCorsOrigins] ([ClientId]);

GO

CREATE INDEX [IX_ClientGrantTypes_ClientId] ON [ClientGrantTypes] ([ClientId]);

GO

CREATE INDEX [IX_ClientIdPRestrictions_ClientId] ON [ClientIdPRestrictions] ([ClientId]);

GO

CREATE INDEX [IX_ClientPostLogoutRedirectUris_ClientId] ON [ClientPostLogoutRedirectUris] ([ClientId]);

GO

CREATE INDEX [IX_ClientRedirectUris_ClientId] ON [ClientRedirectUris] ([ClientId]);

GO

CREATE UNIQUE INDEX [IX_Clients_ClientId] ON [Clients] ([ClientId]);

GO

CREATE INDEX [IX_ClientScopes_ClientId] ON [ClientScopes] ([ClientId]);

GO

CREATE INDEX [IX_ClientSecrets_ClientId] ON [ClientSecrets] ([ClientId]);

GO

CREATE INDEX [IX_IdentityClaims_IdentityResourceId] ON [IdentityClaims] ([IdentityResourceId]);

GO

CREATE UNIQUE INDEX [IX_IdentityResources_Name] ON [IdentityResources] ([Name]);

GO

CREATE INDEX [IX_PersistedGrants_SubjectId_ClientId_Type] ON [PersistedGrants] ([SubjectId], [ClientId], [Type]);

GO