CREATE TABLE [dbo].[ApiScopeClaims]
(
	[Id] int NOT NULL IDENTITY,
    [ApiScopeId] int NOT NULL,
    [Type] nvarchar(200) NOT NULL,
    CONSTRAINT [PK_ApiScopeClaims] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_ApiScopeClaims_ApiScopes_ApiScopeId] FOREIGN KEY ([ApiScopeId]) REFERENCES [ApiScopes] ([Id]) ON DELETE CASCADE
);

GO

CREATE INDEX [IX_ApiScopeClaims_ApiScopeId] ON [ApiScopeClaims] ([ApiScopeId]);

GO
