CREATE TABLE [dbo].[ApiScopes]
(
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
)
ON [HCFabricIdentityData1];

GO

CREATE INDEX [IX_ApiScopes_ApiResourceId] ON [ApiScopes] ([ApiResourceId])
ON [HCFabricIdentityIndex1];

GO

CREATE UNIQUE INDEX [IX_ApiScopes_Name] ON [ApiScopes] ([Name])
ON [HCFabricIdentityIndex1];

GO