CREATE TABLE [dbo].[ClientClaims]
(
	[Id] int NOT NULL IDENTITY,
	[ClientId] int NOT NULL,
	[Type] nvarchar(250) NOT NULL,
	[Value] nvarchar(250) NOT NULL,
	CONSTRAINT [PK_ClientClaims] PRIMARY KEY ([Id]),
	CONSTRAINT [FK_ClientClaims_Clients_ClientId] FOREIGN KEY ([ClientId]) REFERENCES [Clients] ([Id]) ON DELETE CASCADE
);

GO

CREATE INDEX [IX_ClientClaims_ClientId] ON [ClientClaims] ([ClientId]);

GO