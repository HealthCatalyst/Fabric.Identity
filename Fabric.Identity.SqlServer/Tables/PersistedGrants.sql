CREATE TABLE [dbo].[PersistedGrants]
(
	[Key] nvarchar(200) NOT NULL,
	[ClientId] nvarchar(200) NOT NULL,
	[CreationTime] datetime2 NOT NULL,
	[Data] nvarchar(max) NOT NULL,
	[Expiration] datetime2 NULL,
	[SubjectId] nvarchar(200) NULL,
	[Type] nvarchar(50) NOT NULL,
	[CreatedDateTimeUtc] datetime NOT NULL,
	[ModifiedDateTimeUtc] datetime	NULL,
	[CreatedBy] nvarchar(100) NOT NULL,
	[ModifiedBy] nvarchar(100) NULL,
	[IsDeleted] bit default 0 NOT NULL,
	CONSTRAINT [PK_PersistedGrants] PRIMARY KEY ([Key])
)
ON [HCFabricIdentityData1];

GO

CREATE INDEX [IX_PersistedGrants_SubjectId_ClientId_Type] ON [PersistedGrants] ([SubjectId], [ClientId], [Type])
ON [HCFabricIdentityIndex1];

GO