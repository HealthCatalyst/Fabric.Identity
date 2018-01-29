#!/bin/bash

workingDirectory=$1

clientSecretJson=$(<$workingDirectory/variables.txt)
echo $clientSecretJson

installersecret=$(echo $clientSecretJson | jq .installerSecret) 
authorizationclientsecret=$(echo $clientSecretJson | jq .authClientSecret) 

echo "##vso[task.setvariable variable=FABRIC_INSTALLER_SECRET;]$installersecret"
echo "##vso[task.setvariable variable=AUTH_CLIENT_SECRET;]$authorizationclientsecret"