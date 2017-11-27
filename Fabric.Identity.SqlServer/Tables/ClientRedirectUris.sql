CREATE TABLE [dbo].[ClientRedirectUris]
(
	[Id] int NOT NULL IDENTITY,
	[ClientId] int NOT NULL,
	[RedirectUri] nvarchar(2000) NOT NULL,
	CONSTRAINT [PK_ClientRedirectUris] PRIMARY KEY ([Id]),
	CONSTRAINT [FK_ClientRedirectUris_Clients_ClientId] FOREIGN KEY ([ClientId]) REFERENCES [Clients] ([Id]) ON DELETE CASCADE
);

GO

CREATE INDEX [IX_ClientRedirectUris_ClientId] ON [ClientRedirectUris] ([ClientId]);

GO
