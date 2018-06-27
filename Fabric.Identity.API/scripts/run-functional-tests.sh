#!/bin/bash

./start-auth-identity.sh

cd ../../../Fabric.Identity/Fabric.Identity.FunctionalTests
npm install
npm test

cd ../Fabric.Identity.API/scripts
./stop-auth-identity.sh