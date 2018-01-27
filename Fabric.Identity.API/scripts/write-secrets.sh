#!/bin/bash

installerSecretString=$(FABRIC_INSTALLER_SECRET)
authClientSecretString=$(AUTH_CLIENT_SECRET)

json="{\"installerSecret\":\""$installerSecretString"\", \"authClientSecret\":\""$authClientSecretString"\"}"  

echo -e $json
echo $json > $(Build.ArtifactStagingDirectory)"\variables.txt"