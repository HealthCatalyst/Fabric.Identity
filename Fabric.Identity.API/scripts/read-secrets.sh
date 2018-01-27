#!/bin/bash

value=$(</c/hc/variables.txt)
echo "$value"

installersecret=$(echo $value | jq .installerSecret) 
authorizationclientsecret$(echo $value | jq .authClientSecret) 

echo "##vso[task.setvariable variable=FABRIC_INSTALLER_SECRET;]$installersecret"
echo "##vso[task.setvariable variable=AUTH_CLIENT_SECRET;]$authorizationclientsecret"