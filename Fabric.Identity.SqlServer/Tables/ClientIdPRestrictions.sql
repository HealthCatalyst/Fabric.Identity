CREATE TABLE [dbo].[ClientIdPRestrictions]
(
	[Id] int NOT NULL IDENTITY,
	[ClientId] int NOT NULL,
	[Provider] nvarchar(200) NOT NULL,
	CONSTRAINT [PK_ClientIdPRestrictions] PRIMARY KEY ([Id]),
	CONSTRAINT [FK_ClientIdPRestrictions_Clients_ClientId] FOREIGN KEY ([ClientId]) REFERENCES [Clients] ([Id]) ON DELETE CASCADE
)
ON [HCFabricIdentityData1];

GO

CREATE INDEX [IX_ClientIdPRestrictions_ClientId] ON [ClientIdPRestrictions] ([ClientId])
ON [HCFabricIdentityIndex1];

GO