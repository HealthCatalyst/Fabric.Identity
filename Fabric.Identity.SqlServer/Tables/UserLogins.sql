CREATE TABLE [dbo].[UserLogins]
(
	[Id] int NOT NULL IDENTITY,
	[UserId] int NOT NULL,
	[ClientId] nvarchar(200) NOT NULL,
	[LoginDate] datetime NOT NULL,	
	CONSTRAINT [PK_[UserLogins] PRIMARY KEY ([Id]),	
	CONSTRAINT [FK_UserLogins_Users_Id] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id])
);
