#!/bin/bash
couchusername=$1
couchpassword=$2

docker stop functional-couchdb
docker rm functional-couchdb
docker volume rm identity-func-db-data
docker stop functional-authorization
docker rm functional-authorization
docker stop functional-identity
docker rm functional-identity

docker network create functional-tests

docker run -d --name functional-couchdb \
	-p 5984:5984 \
	-e COUCHDB_USER=$couchusername \
	-e COUCHDB_PASSWORD=$couchpassword \
	-v identity-func-db-data://opt/couch/data \
	--network="functional-tests" \
	healthcatalyst/fabric.docker.couchdb
echo "started couchdb"
sleep 15 

docker run -d --name functional-authorization \
	-p 5004:5004 \
	-e COUCHDBSETTINGS__USERNAME=$couchusername \
	-e COUCHDBSETTINGS__PASSWORD=$couchpassword \
	-e COUCHDBSETTINGS__SERVER=http://functional-couchdb:5984 \
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
	-e COUCHDBSETTINGS__USERNAME=$couchusername \
        -e COUCHDBSETTINGS__PASSWORD=$couchpassword \
	-e COUCHDBSETTINGS__SERVER=http://functional-couchdb:5984 \
	-e HOSTINGOPTIONS__STORAGEPROVIDER="CouchDB" \
	--network="functional-tests" \
	identity.functional.api
echo "started api"
sleep 3

cd ../../Fabric.Identity/Fabric.Identity.FunctionalTests
npm install
npm test

docker stop functional-couchdb
docker rm functional-couchdb 
docker volume rm identity-func-db-data
docker stop functional-authorization 
docker rm functional-authorization 
docker stop functional-identity 
docker rm functional-identity
