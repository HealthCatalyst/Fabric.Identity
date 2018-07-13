#!/bin/bash

docker stop functional-authorization
docker rm functional-authorization
docker stop functional-identity
docker rm functional-identity

docker network create functional-tests

docker run -d --name functional-authorization \
	-p 5004:5004 \
	-e STORAGEPROVIDER=InMemory \
	-e IDENTITYSERVERCONFIDENTIALCLIENTSETTINGS__AUTHORITY=http://functional-identity:5001 \
	--network="functional-tests" \
	healthcatalyst/fabric.authorization
echo "started authorization"
sleep 3

cd ..
dotnet publish -o obj/Docker/publish
echo "published api"
docker build -t identity.functional.api .
echo "built image"

docker run -d --name functional-identity \
	-p 5001:5001 \
	-e HOSTINGOPTIONS__STORAGEPROVIDER="InMemory" \
	-e HOSTINGOPTIONS__ALLOWUNSAFEEVAL="true" \
	-e "IssuerUri=http://functional-identity:5001" \
	-e "IDENTITYSERVERCONFIDENTIALCLIENTSETTINGS__AUTHORITY=http://functional-identity:5001" \
	--network="functional-tests" \
	identity.functional.api
echo "started api"
sleep 3

cd ../../Fabric.Identity/Fabric.Identity.FunctionalTests
npm install
npm test

docker stop functional-authorization 
docker rm functional-authorization 
docker stop functional-identity 
docker rm functional-identity