#!/bin/bash

# register registration api
echo "registering Fabric.Registration..."
registrationapiresponse=$(curl -X POST -H "Content-Type: application/json" -d "{ \"name\": \"registration-api\", \"userClaims\": [\"name\", \"email\", \"role\", \"groups\"], \"scopes\": [{ \"name\": \"fabric/identity.manageresources\"}]}" http://localhost:5001/api/apiresource)
echo $registrationapiresponse
echo ""

# register the installer client
echo "registering Fabric.Installer..."
installerresponse=$(curl -X POST -H "Content-Type: application/json" -d "{ \"clientId\": \"fabric-installer\", \"clientName\": \"Fabric Installer\", \"requireConsent\": false, \"allowedGrantTypes\": [\"client_credentials\"], \"allowedScopes\": [\"fabric/identity.manageresources\"]}" http://localhost:5001/api/client)
echo $installerresponse
installersecret=$(echo $installerresponse | grep -oP '(?<="clientSecret":")[^"]*')
echo ""

# get access token for installer
echo "getting access token for installer..."
accesstokenresponse=$(curl http://localhost:5001/connect/token --data "client_id=fabric-installer&grant_type=client_credentials&scope=fabric/identity.manageresources" --data-urlencode "client_secret=$installersecret")
echo $accesstokenresponse
accesstoken=$(echo $accesstokenresponse | grep -oP '(?<="access_token":")[^"]*')
echo ""

# register authorization api
echo "registering Fabric.Authorization..."
authapiresponse=$(curl -X POST -H "Content-Type: application/json" -H "Authorization: Bearer $accesstoken" -d "{ \"name\": \"authorization-api\", \"userClaims\": [\"name\", \"email\", \"role\", \"groups\"], \"scopes\": [{ \"name\": \"fabric/authorization.read\"}, {\"name\": \"fabric/authorization.write\"}, {\"name\":\"fabric/authorization.manageclients\"}]}" http://localhost:5001/api/apiresource)
echo $authapiresponse
echo ""

# register the group fetcher client
echo "registering Fabric.GroupFetcher..."
groupfetcherresponse=$(curl -X POST -H "Content-Type: application/json" -H "Authorization: Bearer $accesstoken" -d "{ \"clientId\": \"fabric-group-fetcher\", \"clientName\": \"Fabric Group Fetcher\", \"requireConsent\": false, \"allowedGrantTypes\": [\"client_credentials\"], \"allowedScopes\": [\"fabric/authorization.manageclients\", \"fabric/authorization.write\"]}" http://localhost:5001/api/client)
echo $groupfetcherresponse
groupfetchersecret=$(echo $groupfetcherresponse | grep -oP '(?<="clientSecret":")[^"]*')
echo ""

# register patient api
echo "registering Fabric.Identity.Samples.API..."
patientapiresponse=$(curl -X POST -H "Content-Type: application/json" -H "Authorization: Bearer $accesstoken" -d "{ \"name\": \"patientapi\", \"userClaims\": [\"name\", \"email\", \"role\", \"groups\"], \"scopes\": [{\"name\":\"patientapi\", \"displayName\":\"Patient API\"}]}" http://localhost:5001/api/apiresource)
echo $patientapiresponse
echo ""

# register mvc client
echo "registering Fabric.Identity.Samples.MVC..."
mvcclientresponse=$(curl -X POST -H "Content-Type: application/json" -H "Authorization: Bearer $accesstoken" -d "{ \"clientId\": \"fabric-mvcsample\", \"clientName\": \"Sample Fabric MVC Client\", \"requireConsent\": false, \"allowedGrantTypes\": [\"hybrid\", \"client_credentials\"], \"redirectUris\": [\"http://localhost:5002/signin-oidc\"], \"postLogoutRedirectUris\": [ \"http://localhost:5002/signout-callback-oidc\"], \"allowOfflineAccess\": true, \"RequireConsent\": false, \"allowedScopes\": [\"openid\", \"profile\", \"fabric.profile\", \"fabric/authorization.read\", \"fabric/authorization.write\", \"patientapi\"]}" http://localhost:5001/api/client)
echo $mvcclientresponse
echo ""

# register angular client
echo "registering Fabric.Identity.Samples.Angular..."
angularclientresponse=$(curl -X POST -H "Content-Type: application/json" -H "Authorization: Bearer $accesstoken" -d "{ \"clientId\": \"fabric-angularsample\", \"clientName\": \"Sample Fabric Angular Client\", \"requireConsent\": false, \"allowedGrantTypes\": [\"implicit\"], \"redirectUris\": [\"http://localhost:4200/oidc-callback.html\", \"http://localhost:4200/silent.html\"], \"postLogoutRedirectUris\": [ \"http://localhost:4200\"], \"allowOfflineAccess\": false, \"AllowAccessTokensViaBrowser\": true, \"AllowedCorsOrigins\":[\"http://localhost:4200\"], \"RequireConsent\": false, \"allowedScopes\": [\"openid\", \"profile\", \"fabric.profile\", \"fabric/authorization.read\", \"fabric/authorization.write\", \"fabric/authorization.manageclients\", \"patientapi\"]}" http://localhost:5001/api/client)
echo $angularclientresponse
echo ""

accesstoken=""

echo "The Fabric.Installer client secret is:"
echo $installersecret
echo "You need this secret if you want to register additional API resources or clients."
echo ""

echo "The Fabric.GroupFetcher client secret is:"
echo $groupfetchersecret
echo "You need this secret so the group fetcher can authenticate to get save groups."
echo ""

echo "Update the Fabric.Authorization appsettings.json IdentityServerConfidentialClientSettings.ClientSecret value to:"
echo $authapiresponse | grep -oP '(?<="apiSecret":")[^"]*'
echo ""

echo "Update the Fabric.Identity.Samples.API appsettings.json IdentityServerConfidentialClientSettings.ClientSecret value to:"
echo $patientapiresponse | grep -oP '(?<="apiSecret":")[^"]*'
echo ""

echo "Update the Fabric.Identity.Samples.MVC appsettings.json IdentityServerConfidentialClientSettings.ClientSecret value to:"
echo $mvcclientresponse | grep -oP '(?<="clientSecret":")[^"]*'
echo ""
