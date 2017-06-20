#!/bin/bash

# register authorization api
echo "registering Fabric.Authorization..."
authapiresponse=$(curl -X POST -H "Content-Type: application/json" -d "{ \"name\": \"authorization-api\", \"userClaims\": [\"name\", \"email\", \"role\", \"groups\"], \"scopes\": [{ \"name\": \"fabric/authorization.read\"}, {\"name\": \"fabric/authorization.write\"}, {\"name\":\"fabric/authorization.manageclients\"}]}" http://localhost:5001/api/apiresource)
echo ""

# register patient api
echo "registering Fabric.Identity.Samples.API..."
patientapiresponse=$(curl -X POST -H "Content-Type: application/json" -d "{ \"name\": \"patientapi\", \"userClaims\": [\"name\", \"email\", \"role\", \"groups\"], \"scopes\": [{\"name\":\"patientapi\", \"displayName\":\"Patient API\"}]}" http://localhost:5001/api/apiresource)
echo ""

# register mvc client
echo "registering Fabric.Identity.Samples.MVC..."
mvcclientresponse=$(curl -X POST -H "Content-Type: application/json" -d "{ \"clientId\": \"fabric-mvcsample\", \"clientName\": \"Sample Fabric MVC Client\", \"requireConsent\": false, \"allowedGrantTypes\": [\"hybrid\", \"client_credentials\"], \"redirectUris\": [\"http://localhost:5002/signin-oidc\"], \"postLogoutRedirectUris\": [ \"http://localhost:5002/signout-callback-oidc\"], \"allowOfflineAccess\": true, \"RequireConsent\": false, \"allowedScopes\": [\"openid\", \"profile\", \"fabric.profile\", \"fabric/authorization.read\", \"fabric/authorization.write\", \"patientapi\"]}" http://localhost:5001/api/client)
echo ""

# register angular client
echo "registering Fabric.Identity.Samples.Angular..."
angularclientresponse=$(curl -X POST -H "Content-Type: application/json" -d "{ \"clientId\": \"fabric-angularsample\", \"clientName\": \"Sample Fabric Angular Client\", \"requireConsent\": false, \"allowedGrantTypes\": [\"implicit\"], \"redirectUris\": [\"http://localhost:4200/oidc-callback.html\", \"http://localhost:4200/silent.html\"], \"postLogoutRedirectUris\": [ \"http://localhost:4200\"], \"allowOfflineAccess\": false, \"AllowAccessTokensViaBrowser\": true, \"AllowedCorsOrigins\":[\"http://localhost:4200\"], \"RequireConsent\": false, \"allowedScopes\": [\"openid\", \"profile\", \"fabric.profile\", \"fabric/authorization.read\", \"fabric/authorization.write\", \"fabric/authorization.manageclients\", \"patientapi\"]}" http://localhost:5001/api/client)
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
