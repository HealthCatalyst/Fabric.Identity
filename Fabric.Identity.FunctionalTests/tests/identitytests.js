var chakram = require("chakram");
var expect = require("chakram").expect;

describe("identity tests", function () {
    var baseAuthUrl = process.env.BASE_AUTH_URL;
    var baseIdentityUrl = process.env.BASE_IDENTITY_URL;

    if (!baseAuthUrl) {
        baseAuthUrl = "http://localhost:5004";
    }

    if (!baseIdentityUrl) {
        baseIdentityUrl = "http://localhost:5001";
    }

    var installerSecret = "";
    var newClientSecret = "";

    var authRequestOptions = {
        headers: {
            "Content-Type": "application/json",
            "Accept": "application/json",
            "Authorization": ""
        }
    }

    var requestOptions = {
        headers: {
            "content-type": "application/json"
        }
    }

    var identityClientFuncTest = {
        "clientId": "func-test",
        "clientName": "Functional Test Client",
        "requireConsent": "false",
        "allowedGrantTypes": ["client_credentials", "password"],
        "allowedScopes": [
            "fabric/identity.manageresources",
            "fabric/authorization.read",
            "fabric/authorization.write",
            "openid",
            "profile"
        ]
    }

    var authClientFuncTest = {
        "id": "func-test",
        "name": "Functional Test Client",
        "topLevelSecurableItem": { "name": "func-test" }
    }

    var patientApi = {
        "name": "patientapi",
        "userClaims": [
            "name",
            "email",
            "role",
            "groups"
        ],
        "scopes": [{ "name": "patientapi", "displayName": "Patient API" }]
    }

    function registerAuthorizationApi() {
        var authApiResource = {
            "name": "authorization-api",
            "userClaims": ["name", "email", "role", "groups"],
            "scopes": [
                { "name": "fabric/authorization.read" },
                { "name": "fabric/authorization.write" },
                { "name": "fabric/authorization.manageclients" }
            ]
        }

        return chakram.post(baseIdentityUrl + "/api/apiresource", authApiResource, authRequestOptions);
    }

    function registerRegistrationApi() {
        var registrationApi = {
            "name": "registration-api",
            "userClaims": ["name", "email", "role", "groups"],
            "scopes": [{ "name": "fabric/identity.manageresources" }]
        }

        return chakram.post(baseIdentityUrl + "/api/apiresource", registrationApi, requestOptions);
    }

    function registerInstallerClient() {
        var installerClient = {
            "clientId": "fabric-installer",
            "clientName": "Fabric Installer",
            "requireConsent": "false",
            "allowedGrantTypes": ["client_credentials"],
            "allowedScopes": [
                "fabric/identity.manageresources",
                "fabric/authorization.read",
                "fabric/authorization.write",
                "fabric/authorization.manageclients"]
        }

        return chakram.post(baseIdentityUrl + "/api/client", installerClient, requestOptions);
    }

    function getAccessToken(clientData) {
        return chakram.post(baseIdentityUrl + "/connect/token", null, clientData)
            .then(function (postResponse) {
                var accessToken = "Bearer " + postResponse.body.access_token;
                return accessToken;
            });
    }

    function getAccessTokenForInstaller(installerClientSecret) {
        var postData = {
            form: {
                "client_id": "fabric-installer",
                "client_secret": installerClientSecret,
                "grant_type": "client_credentials",
                "scope": "fabric/identity.manageresources fabric/authorization.read fabric/authorization.write fabric/authorization.manageclients"
            }
        };

        return getAccessToken(postData);
    }

    function getAccessTokenForFuncTestClient(newAuthClientSecret) {
        var clientData = {
            form: {
                "client_id": "func-test",
                "client_secret": newAuthClientSecret,
                "grant_type": "client_credentials",
                "scope": "fabric/authorization.read fabric/authorization.write"
            }
        }

        return getAccessToken(clientData);
    }

    function bootstrapIdentityServer() {
        return registerRegistrationApi()
            .then(registerAuthorizationApi)
            .then(registerInstallerClient)
            .then(function (postResponse) {
                return postResponse.body.clientSecret;
            })
            .then(function (installerClientSecret) {
                installerSecret = installerClientSecret;
                return getAccessTokenForInstaller(installerSecret);
            })
            .then(function (retrievedAccessToken) {
                authRequestOptions.headers.Authorization = retrievedAccessToken;
            });
    }

    before("running before", function () {
        this.timeout(5000);
        return bootstrapIdentityServer();
    });

    describe("register client", function () {
        it("should register a client", function () {
            return chakram.post(baseIdentityUrl + "/api/client", identityClientFuncTest, authRequestOptions)
                .then(function (clientResponse) {
                    expect(clientResponse).to.have.status(201);
                    expect(clientResponse).to.comprise.of.json({ clientId: "func-test" });
                    newClientSecret = clientResponse.body.clientSecret;
                });
        });
    });

    describe("register api", function () {
        it("should register an api", function () {
            return chakram.post(baseIdentityUrl + "/api/apiresource", patientApi, authRequestOptions)
                .then(function (clientResponse) {
                    expect(clientResponse).to.have.status(201);
                    expect(clientResponse).to.comprise.of.json({ name: "patientapi" });
                    newClientSecret = clientResponse.body.clientSecret;
                });
        });
    });

    describe("call api with access token", function () {
        it("should be able to add a client to authorization api", function () {
            return chakram.post(baseAuthUrl + "/clients", authClientFuncTest, authRequestOptions)
                .then(function (clientResponse) {
                    expect(clientResponse).to.have.status(201);
                });
        });
    });

    describe("authenticate user", function(){
        it("should be able to authenticate user using password flow", function(){
           
            //hit the token endpoint for identity with the username and password of the user
            var stringToEncode = "func-test:" + newClientSecret;
            var encodedData = new Buffer(stringToEncode).toString("base64");

            var postData = {
                        form: {
                            "grant_type": "password",
                            "username": "bob",
                            "password": "bob"
                        },
                         headers: {
                            "content-type": "application/x-www-form-urlencoded",
                            "Authorization": "Basic " + encodedData
                        }   
                    };      

            return getAccessToken(postData)
            .then(function(accessToken){
                expect(accessToken).to.not.be.null;                
            });         
        });

        it("should be able to authenticate using client credentials flow", function(){
            return getAccessTokenForInstaller(installerSecret)
            .then(function(accessToken){
                expect(accessToken).to.not.be.null;
            });
        });
    });
});