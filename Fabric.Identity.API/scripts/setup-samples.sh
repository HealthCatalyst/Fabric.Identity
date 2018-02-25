#!/bin/bash

if [ $1 ]; then
	identitybaseurl=$1
fi

if ! [ $identitybaseurl ]; then
	identitybaseurl=http://localhost:5001
fi

# register registration api
echo "registering Fabric.Registration..."
registrationapiresponse=$(curl -X POST -H "Content-Type: application/json" -d "{ \"name\": \"registration-api\", \"userClaims\": [\"name\", \"email\", \"role\", \"groups\"], \"scopes\": [{ \"name\": \"fabric/identity.manageresources\"}, { \"name\": \"fabric/identity.read\"}, { \"name\": \"fabric/identity.searchusers\"}]}" $identitybaseurl/api/apiresource)
echo $registrationapiresponse
echo ""

# register the installer client
echo "registering Fabric.Installer..."
installerresponse=$(curl -X POST -H "Content-Type: application/json" -d "{ \"clientId\": \"fabric-installer\", \"clientName\": \"Fabric Installer\", \"requireConsent\": false, \"allowedGrantTypes\": [\"client_credentials\"], \"allowedScopes\": [\"fabric/identity.manageresources\", \"fabric/identity.read\", \"fabric/authorization.read\", \"fabric/authorization.write\", \"fabric/authorization.manageclients\"]}" $identitybaseurl/api/client)
echo $installerresponse
installersecret=$(echo $installerresponse | grep -oP '(?<="clientSecret":")[^"]*')
echo ""

# get access token for installer
echo "getting access token for installer..."
accesstokenresponse=$(curl $identitybaseurl/connect/token --data "client_id=fabric-installer&grant_type=client_credentials&scope=fabric/identity.manageresources" --data-urlencode "client_secret=$installersecret")
echo $accesstokenresponse
accesstoken=$(echo $accesstokenresponse | grep -oP '(?<="access_token":")[^"]*')
echo ""

# register authorization api
echo "registering Fabric.Authorization..."
authapiresponse=$(curl -X POST -H "Content-Type: application/json" -H "Authorization: Bearer $accesstoken" -d "{ \"name\": \"authorization-api\", \"userClaims\": [\"name\", \"email\", \"role\", \"groups\"], \"scopes\": [{ \"name\": \"fabric/authorization.read\"}, {\"name\": \"fabric/authorization.write\"}, {\"name\":\"fabric/authorization.manageclients\"}]}" $identitybaseurl/api/apiresource)
echo $authapiresponse
echo ""

# register the fabric authorization client
echo "registering Fabric.Authorization client..."
authorizationclientresponse=$(curl -X POST -H "Content-Type: application/json" -H "Authorization: Bearer $accesstoken" -d "{ \"clientId\": \"fabric-authorization-client\", \"clientName\": \"Fabric Authorization Client\", \"requireConsent\": false, \"allowedGrantTypes\": [\"client_credentials\"], \"allowedScopes\": [\"fabric/identity.read\", \"fabric/identity.searchusers\"]}" $identitybaseurl/api/client)
echo $authorizationclientresponse
authorizationclientsecret=$(echo $authorizationclientresponse | grep -oP '(?<="clientSecret":")[^"]*')
echo ""

# register the group fetcher client
echo "registering Fabric.GroupFetcher..."
groupfetcherresponse=$(curl -X POST -H "Content-Type: application/json" -H "Authorization: Bearer $accesstoken" -d "{ \"clientId\": \"fabric-group-fetcher\", \"clientName\": \"Fabric Group Fetcher\", \"requireConsent\": false, \"allowedGrantTypes\": [\"client_credentials\"], \"allowedScopes\": [\"fabric/authorization.manageclients\", \"fabric/authorization.write\"]}" $identitybaseurl/api/client)
echo $groupfetcherresponse
groupfetchersecret=$(echo $groupfetcherresponse | grep -oP '(?<="clientSecret":")[^"]*')
echo ""

# register the identity provider search service
echo "registering Fabric.IdentityProviderSearchService API..."
idpsearchserviceresponse=$(curl -X POST -H "Content-Type: application/json" -H "Authorization: Bearer $accesstoken" -d "{ \"name\": \"idpsearch-api\", \"userClaims\": [\"name\", \"email\", \"role\", \"groups\"], \"scopes\": [{ \"name\": \"fabric/idprovider.searchusers\"}]}" $identitybaseurl/api/apiresource)
echo $idpsearchserviceresponse
echo ""

#register identity client
echo "registering Fabric.Identity as a client"
identityclientresponse=$(curl -X POST -H "Content-Type: application/json" -H "Authorization: Bearer $accesstoken" -d "{ \"clientId\": \"fabric-identity-client\", \"clientName\": \"Fabric Identity Client\", \"requireConsent\": false, \"allowedGrantTypes\": [\"client_credentials\"], \"allowedScopes\": [\"fabric/idprovider.searchusers\"]}" $identitybaseurl/api/client)
echo $identityclientresponse
identityclientsecret=$(echo $identityclientresponse | grep -oP '(?<="clientSecret":")[^"]*')
echo ""

# register patient api
echo "registering Fabric.Identity.Samples.API..."
patientapiresponse=$(curl -X POST -H "Content-Type: application/json" -H "Authorization: Bearer $accesstoken" -d "{ \"name\": \"patientapi\", \"userClaims\": [\"name\", \"email\", \"role\", \"groups\"], \"scopes\": [{\"name\":\"patientapi\", \"displayName\":\"Patient API\"}]}" $identitybaseurl/api/apiresource)
echo $patientapiresponse
echo ""

# register mvc client
echo "registering Fabric.Identity.Samples.MVC..."
mvcclientresponse=$(curl -X POST -H "Content-Type: application/json" -H "Authorization: Bearer $accesstoken" -d "{ \"clientId\": \"fabric-mvcsample\", \"clientName\": \"Sample Fabric MVC Client\", \"requireConsent\": false, \"allowedGrantTypes\": [\"hybrid\", \"client_credentials\"], \"redirectUris\": [\"http://localhost:5002/signin-oidc\"], \"postLogoutRedirectUris\": [ \"http://localhost:5002/signout-callback-oidc\"], \"allowOfflineAccess\": true, \"RequireConsent\": false, \"allowedScopes\": [\"openid\", \"profile\", \"fabric.profile\", \"fabric/authorization.read\", \"fabric/authorization.write\", \"patientapi\"]}" $identitybaseurl/api/client)
echo $mvcclientresponse
echo ""

# register angular client
echo "registering Fabric.Identity.Samples.Angular..."
angularclientresponse=$(curl -X POST -H "Content-Type: application/json" -H "Authorization: Bearer $accesstoken" -d "{ \"clientId\": \"fabric-angularsample\", \"clientName\": \"Sample Fabric Angular Client\", \"requireConsent\": false, \"allowedGrantTypes\": [\"implicit\"], \"redirectUris\": [\"http://localhost:4200/oidc-callback.html\", \"http://localhost:4200/silent.html\"], \"postLogoutRedirectUris\": [ \"http://localhost:4200\"], \"allowOfflineAccess\": false, \"AllowAccessTokensViaBrowser\": true, \"AllowedCorsOrigins\":[\"http://localhost:4200\"], \"RequireConsent\": false, \"allowedScopes\": [\"openid\", \"profile\", \"fabric.profile\", \"fabric/authorization.read\", \"fabric/authorization.write\", \"fabric/authorization.manageclients\", \"patientapi\", \"fabric/identity.manageresources\", \"fabric/identity.read\"]}" $identitybaseurl/api/client)
echo $angularclientresponse
echo ""

accesstoken=""

echo "The Fabric.Installer client secret is:"
echo "\"installerSecret\":\"$installersecret\""
echo "You need this secret if you want to register additional API resources or clients."
echo "##vso[task.setvariable variable=FABRIC_INSTALLER_SECRET;]$installersecret"
echo ""

echo "The Fabric.GroupFetcher client secret is:"
echo "\"groupFetcherSecret\":\"$groupfetchersecret\""
echo "You need this secret so the group fetcher can authenticate to get and save groups."
echo ""

echo "Fabric.Authorization api secret:"
authapisecret=$(echo $authapiresponse | grep -oP '(?<="apiSecret":")[^"]*')
echo "\"authApiSecret\":\"$authapisecret\""
echo ""

echo "Update the Fabric.Authorization appsettings.json IdentityServerConfidentialClientSettings.ClientSecret:"
echo "\"authClientSecret\":\"$authorizationclientsecret\""
echo "##vso[task.setvariable variable=AUTH_CLIENT_SECRET;]$authorizationclientsecret"
echo ""

echo "Update the Fabric.Identity appsettings.json IdentityServerConfidentialClientSettings.ClientSecret:"
echo "\"identityClientSecret\":\"$identityclientsecret\""
echo "##vso[task.setvariable variable=IDENTITY_CLIENT_SECRET;]$identityclientsecret"
echo ""

echo "Update the Fabric.Identity.Samples.API appsettings.json IdentityServerConfidentialClientSettings.ClientSecret:"
patientapisecret=$(echo $patientapiresponse | grep -oP '(?<="apiSecret":")[^"]*')
echo "\"patientApiSecret\":\"$patientapisecret\""
echo ""

echo "Update the Fabric.Identity.Samples.MVC appsettings.json IdentityServerConfidentialClientSettings.ClientSecret:"
mvcclientsecret=$(echo $mvcclientresponse | grep -oP '(?<="clientSecret":")[^"]*')
echo "\"mvcClientSecret\":\"$mvcclientsecret\""
echo ""

