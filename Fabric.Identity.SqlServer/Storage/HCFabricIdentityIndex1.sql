/*
Do not change the database path or name variables.
Any sqlcmd variables will be properly substituted during 
build and deployment.
*/
ALTER DATABASE [$(DatabaseName)]
	ADD FILEGROUP [HCFabricIdentityIndex1]

	GO

ALTER DATABASE [$(DatabaseName)]
	ADD FILE
	(
		NAME = [HCFabricIdentityIndex1File1],
		FILENAME = '$(FabricIdentityDataMountPoint)\HC$(DatabaseName)Index1File1.ndf',
		SIZE = 500MB,
		MAXSIZE = 2GB,
		FILEGROWTH = 100MB
	)

TO FILEGROUP [HCFabricIdentityIndex1];
GO
