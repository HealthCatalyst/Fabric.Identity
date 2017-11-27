CREATE TABLE [dbo].[IdentityClaims]
(
	[Id] int NOT NULL IDENTITY,
	[IdentityResourceId] int NOT NULL,
	[Type] nvarchar(200) NOT NULL,
	CONSTRAINT [PK_IdentityClaims] PRIMARY KEY ([Id]),
	CONSTRAINT [FK_IdentityClaims_IdentityResources_IdentityResourceId] FOREIGN KEY ([IdentityResourceId]) REFERENCES [IdentityResources] ([Id]) ON DELETE CASCADE
);

GO

CREATE INDEX [IX_IdentityClaims_IdentityResourceId] ON [IdentityClaims] ([IdentityResourceId]);

GO
