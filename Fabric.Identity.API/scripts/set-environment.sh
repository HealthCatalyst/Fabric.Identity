#!/bin/bash
IP=$(docker inspect -f '{{range .NetworkSettings.Networks}}{{.IPAddress}}{{end}}' $IDENTITYCONTAINERNAME)
echo "Identity Container running on $IP"
echo "##vso[task.setvariable variable=FabricIdentityBaseUrl;]http://$IP:5001"
echo "Identity Server URL (FABRICIDENTITYBASEURL): $FABRICIDENTITYBASEURL"
echo "Identity Server URL (FabricIdentityBaseUrl): $FabricIdentityBaseUrl"
