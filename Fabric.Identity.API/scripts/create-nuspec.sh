#!/bin/bash		
version=$1		
echo "current directory: ${PWD}"		
echo "version set to: $version"		
cat > Fabric.Identity.API.nuspec << EOF		
<?xml version="1.0" encoding="utf-8"?>		
<package xmlns="http://schemas.microsoft.com/packaging/2010/07/nuspec.xsd">		
    <metadata> 		
        <!-- Required elements-->		
        <id>Fabric.Identity.InstallPackage</id>		
        <version>$version</version>		
        <authors>Health Catalyst</authors>		
        <owners>Health Catalyst</owners>		
        <requireLicenseAcceptance>false</requireLicenseAcceptance>		
        <description>Install package for Fabric.Identity</description>		
    </metadata>		
    <files>		
		<file src="Fabric.Identity.API.zip" target="build" />
		<file src="Install-Identity-Windows.ps1" target="build" />
		<file src="Fabric-Install-Utilities.psm1" target="build" />
		<file src="install.config" target="build" />
    </files>		
</package>		
EOF
