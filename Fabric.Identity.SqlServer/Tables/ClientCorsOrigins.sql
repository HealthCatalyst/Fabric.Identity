CREATE TABLE [dbo].[ClientCorsOrigins]
(
	[Id] int NOT NULL IDENTITY,
	[ClientId] int NOT NULL,
	[Origin] nvarchar(150) NOT NULL,
	CONSTRAINT [PK_ClientCorsOrigins] PRIMARY KEY ([Id]),
	CONSTRAINT [FK_ClientCorsOrigins_Clients_ClientId] FOREIGN KEY ([ClientId]) REFERENCES [Clients] ([Id]) ON DELETE CASCADE
)
ON [HCFabricIdentityData1];

GO

CREATE INDEX [IX_ClientCorsOrigins_ClientId] ON [ClientCorsOrigins] ([ClientId])
ON [HCFabricIdentityIndex1];

GO