CREATE TABLE [dbo].[ClientGrantTypes]
(
	[Id] int NOT NULL IDENTITY,
    [ClientId] int NOT NULL,
    [GrantType] nvarchar(250) NOT NULL,
    CONSTRAINT [PK_ClientGrantTypes] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_ClientGrantTypes_Clients_ClientId] FOREIGN KEY ([ClientId]) REFERENCES [Clients] ([Id]) ON DELETE CASCADE
);

GO

CREATE INDEX [IX_ClientGrantTypes_ClientId] ON [ClientGrantTypes] ([ClientId]);

GO