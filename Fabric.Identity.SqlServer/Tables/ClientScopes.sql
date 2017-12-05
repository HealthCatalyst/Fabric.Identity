CREATE TABLE [dbo].[ClientScopes]
(
	[Id] int NOT NULL IDENTITY,
	[ClientId] int NOT NULL,
	[Scope] nvarchar(200) NOT NULL,
	CONSTRAINT [PK_ClientScopes] PRIMARY KEY ([Id]),
	CONSTRAINT [FK_ClientScopes_Clients_ClientId] FOREIGN KEY ([ClientId]) REFERENCES [Clients] ([Id]) ON DELETE CASCADE
)
ON [HCFabricIdentityData1];

GO

CREATE INDEX [IX_ClientScopes_ClientId] ON [ClientScopes] ([ClientId])
ON [HCFabricIdentityIndex1];

GO