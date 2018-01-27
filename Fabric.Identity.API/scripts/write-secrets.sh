#!/bin/bash

set installerSecretString=%~1
set authClientSecretString=%~2
set stagingDirectory=%~3

json="{\"installerSecret\":\""$installerSecretString"\", \"authClientSecret\":\""$authClientSecretString"\"}"  

echo -e $json
echo $json > $stagingDirectory"\variables.txt"