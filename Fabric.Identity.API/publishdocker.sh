#!/bin/bash
read -n 1 -p 'Are you sure you want to publish to dockerhub?'
docker stop fabric.identity
docker rm fabric.identity

dotnet publish --configuration Release --output obj/Docker/publish
docker build -t healthcatalyst/fabric.identity .
docker push healthcatalyst/fabric.identity

echo Press any key to exit
read -n 1
