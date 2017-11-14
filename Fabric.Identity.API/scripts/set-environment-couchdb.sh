#!/bin/bash

IP=$(docker inspect -f '{{range .NetworkSettings.Networks}}{{.IPAddress}}{{end}}' $COUCHDBCONTAINERNAME)
echo "CouchDb Container running on $IP"
echo "##vso[task.setvariable variable=CouchDbSettings__Server;]http://$IP:5984"
echo "CouchDb Server URL: $COUCHDBSETTINGS__SERVER"

IP=$(docker inspect -f '{{range .NetworkSettings.Networks}}{{.IPAddress}}{{end}}' $OPENLDAPCONTAINERNAME)
echo "OpenLdap Container running on $IP"
echo "##vso[task.setvariable variable=LdapSettings__Server;]$IP"
echo "OpenLdap Server: $LDAPSETTINGS__SERVER"

IP=$(docker inspect -f '{{range .NetworkSettings.Networks}}{{.IPAddress}}{{end}}' $SQLSERVERCONTAINERNAME)
echo "SqlServer Container running on $IP"
echo "##vso[task.setvariable variable=SqlServerSettings__Server;]$IP"
echo "SqlServer: $SQLSERVERSETTINGS__SERVER"