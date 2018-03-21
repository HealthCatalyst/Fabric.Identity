#!/bin/bash

docker stop identity.integration.openldap
docker rm identity.integration.openldap
docker run -d --name identity.integration.openldap -d -p 389:389 -e LDAP_ADMIN_PASSWORD=password healthcatalyst/fabric.docker.openldap

sleep 3
"/C/Program Files (x86)/Microsoft Visual Studio/2017/Professional/MSBuild/15.0/Bin/MSBuild.exe"  ../../Fabric.Identity.SqlServer/Fabric.Identity.SqlServer.sqlproj
cp ../../Fabric.Identity.SqlServer/bin/Debug/Fabric.Identity.SqlServer_Create.sql ../../Fabric.Identity.IntegrationTests/bin/Debug/netcoreapp1.1/Fabric.Identity.SqlServer_Create.sql

dotnet test ../../Fabric.Identity.IntegrationTests/Fabric.Identity.IntegrationTests.csproj

docker stop identity.integration.openldap 
docker rm identity.integration.openldap
