#!/bin/bash

authserver=$1
authport=$2
identityserver=$3
identityport=$4

identityprotocol="http"

if [ $5 ]; then
    identityprotocol=$5
fi

cat > user.properties << EOF
identityserver=$identityserver
identityport=$identityport
identityprotocol=$identityprotocol
authserver=$authserver
authport=$authport
EOF
