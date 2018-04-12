  DECLARE @EdwAdminUsers TABLE
  (
	IdentityID INT NOT NULL,
	IdentityNM VARCHAR(255) NOT NULL,
	RoleNM VARCHAR(255) NOT NULL
  )

  DECLARE @DosAdminRoleId VARCHAR(255)


BEGIN TRY
	BEGIN TRANSACTION;

	-- Get the members of IdentityBASE who have the 'EDW Admin' role
	INSERT INTO @EdwAdminUsers
	SELECT i.IdentityID, i.IdentityNM, r.RoleNM
	FROM [EDWAdmin].[CatalystAdmin].[RoleBASE] r
		INNER JOIN EDWAdmin.CatalystAdmin.IdentityRoleBASE ir ON r.RoleID = ir.RoleID
		INNER JOIN EDWAdmin.CatalystAdmin.IdentityBASE i on ir.IdentityID = i.IdentityID
	WHERE RoleNM = 'EDW Admin'

	-- Insert the users from IdentityRoleBASE into dbo.Users that don't already exist in dbo.Users
	INSERT INTO [Authorization].[dbo].[Users] (SubjectId, IdentityProvider, CreatedBy, CreatedDateTimeUtc, IsDeleted)
	SELECT i.IdentityNM, 'Windows', 'fabric-installer', getUtcDate(), 0
	FROM @EdwAdminUsers i
	LEFT JOIN [Authorization].[dbo].Users u
	  ON i.IdentityNM = u.SubjectId
	WHERE u.SubjectId IS NULL

	-- Get the dosadmin RoldId
	SET @DosAdminRoleId = (SELECT RoleId from [Authorization].[dbo].[Roles] WHERE Name = 'dosadmin')

	-- Insert these users into RoleUsers table associated with the dosadmin role
	INSERT INTO [Authorization].[dbo].[RoleUsers] (CreatedBy, CreatedDateTimeUtc, RoleId, IdentityProvider, IsDeleted, SubjectId)
	SELECT 'fabric-installer', getUtcDate(), @DosAdminRoleId, 'windows', 0, i.IdentityNM
	FROM @EdwAdminUsers i

	 SELECT * FROM [Authorization].[dbo].[RoleUsers]

	COMMIT TRANSACTION;
END TRY
BEGIN CATCH
	ROLLBACK TRANSACTION;
	THROW;
END CATCH

