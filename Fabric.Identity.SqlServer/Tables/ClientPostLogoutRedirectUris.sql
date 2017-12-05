CREATE TABLE [dbo].[ClientPostLogoutRedirectUris]
(
	[Id] int NOT NULL IDENTITY,
	[ClientId] int NOT NULL,
	[PostLogoutRedirectUri] nvarchar(2000) NOT NULL,
	CONSTRAINT [PK_ClientPostLogoutRedirectUris] PRIMARY KEY ([Id]),
	CONSTRAINT [FK_ClientPostLogoutRedirectUris_Clients_ClientId] FOREIGN KEY ([ClientId]) REFERENCES [Clients] ([Id]) ON DELETE CASCADE
)
ON [HCFabricIdentityData1];

GO

CREATE INDEX [IX_ClientPostLogoutRedirectUris_ClientId] ON [ClientPostLogoutRedirectUris] ([ClientId])
ON [HCFabricIdentityIndex1];

GO