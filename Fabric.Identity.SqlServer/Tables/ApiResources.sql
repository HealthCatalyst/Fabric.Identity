CREATE TABLE [dbo].[ApiResources]
(
	[Id] int NOT NULL IDENTITY,
    [Description] nvarchar(1000) NULL,
    [DisplayName] nvarchar(200) NULL,
    [Enabled] bit NOT NULL,
    [Name] nvarchar(200) NOT NULL,
	[CreatedDateTimeUtc] datetime NOT NULL,
	[ModifiedDateTimeUtc] datetime	NULL,
	[CreatedBy] nvarchar(100) NOT NULL,
	[ModifiedBy] nvarchar(100) NULL,
	[IsDeleted] bit default 0 NOT NULL,
    CONSTRAINT [PK_ApiResources] PRIMARY KEY ([Id])
);

GO

CREATE UNIQUE INDEX [IX_ApiResources_Name] ON [ApiResources] ([Name]);

GO