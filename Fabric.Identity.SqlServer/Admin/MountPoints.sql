/*
Do not change the database path or name variables.
Any sqlcmd variables will be properly substituted during 
build and deployment.
*/

ALTER DATABASE [$(DatabaseName)]
    ADD FILE (NAME = [HCFabricIdentityData1File1], FILENAME = '$(FabricIdentityDataMountPoint)\HC$(DatabaseName)Data1File1.ndf', SIZE = 100 MB) TO FILEGROUP [HCFabricIdentityData1];

GO

ALTER DATABASE [$(DatabaseName)]
    ADD LOG FILE (NAME = [HCFabricIdentityLogFile1], FILENAME = '$(FabricidentityLogFileMountPoint)\HC$(DatabaseName)LogFile1.ldf', SIZE = 100 MB);

GO

ALTER DATABASE [$(DatabaseName)]
    ADD FILEGROUP [HCFabricIdentityData1];

GO