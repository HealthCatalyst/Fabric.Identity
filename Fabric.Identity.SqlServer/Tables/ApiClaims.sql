CREATE TABLE [dbo].[ApiClaims]
(
	[Id] int NOT NULL IDENTITY,
	[ApiResourceId] int NOT NULL,
	[Type] nvarchar(200) NOT NULL,
	CONSTRAINT [PK_ApiClaims] PRIMARY KEY ([Id]),
	CONSTRAINT [FK_ApiClaims_ApiResources_ApiResourceId] FOREIGN KEY ([ApiResourceId]) REFERENCES [ApiResources] ([Id]) ON DELETE CASCADE
);

GO

CREATE INDEX [IX_ApiClaims_ApiResourceId] ON [ApiClaims] ([ApiResourceId]);

GO
