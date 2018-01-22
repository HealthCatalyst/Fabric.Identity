CREATE TABLE [dbo].[UserClaims]
(
	[Id] int NOT NULL IDENTITY,
	[UserId] int NOT NULL,
	[Type] nvarchar(200) NOT NULL,
	[Value] nvarchar(400) NULL,
	CONSTRAINT [PK_UserClaims] PRIMARY KEY ([Id]),
	CONSTRAINT [FK_UserClaims_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
)
ON [HCFabricIdentityData1];

GO