CREATE TABLE [dbo].[Users]
(
	[Id] int NOT NULL IDENTITY,
	[SubjectId] nvarchar(200) NOT NULL,
	[ProviderName] nvarchar(200) NOT NULL,
	[ComputedUserId] AS SubjectId + ':' + ProviderName,
	[FirstName] nvarchar(200) NULL,
	[MiddleName] nvarchar(200) NULL,
	[LastName] nvarchar(200) NULL,
	[Username] nvarchar(200) NOT NULL,
	[CreatedDateTimeUtc] datetime NOT NULL,
	[ModifiedDateTimeUtc] datetime	NULL,
	[CreatedBy] nvarchar(100) NOT NULL,
	[ModifiedBy] nvarchar(100) NULL,
	CONSTRAINT [PK_[Users] PRIMARY KEY ([Id])
);
