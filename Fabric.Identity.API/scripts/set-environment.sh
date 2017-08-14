#!/bin/bash

IP=$(docker inspect -f '{{range .NetworkSettings.Networks}}{{.IPAddress}}{{end}}' $IDENTITYCONTAINERNAME)
echo "Identity Container running on $IP"
