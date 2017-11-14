CREATE TABLE [dbo].[ClientSecrets]
(
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

CREATE INDEX [IX_ClientSecrets_ClientId] ON [ClientSecrets] ([ClientId]);

GO