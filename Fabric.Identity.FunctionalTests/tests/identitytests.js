var chakram = require("chakram");
var expect = require("chakram").expect;

var webdriver = require("selenium-webdriver"),
By = webdriver.By,
until = webdriver.until;

var url = require("url");
var qs = require("qs");

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
        "allowedGrantTypes": ["client_credentials", "password", "implicit"],
        "allowedScopes": [
            "fabric/identity.manageresources",
            "fabric/authorization.read",
            "fabric/authorization.write",
            "openid",
            "profile"
        ],
        "redirectUris": [baseIdentityUrl],
        "allowAccessTokensViaBrowser": true,
        "allowedCorsOrigins": ["http://localhost:4200"]
    }

    var identityClientHybridFuncTest = {
        "clientId": "func-test-hybrid",
        "clientName": "Functional Test Hybrid Client",
        "requireConsent": "false",
        "allowedGrantTypes": ["hybrid"],
        "allowedScopes": [
            "fabric/identity.manageresources",            
            "openid",
            "profile"            
        ],
        "allowOfflineAccess": true,
        "redirectUris": [baseIdentityUrl],
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

    function getAccessTokenForFuncTestHybridClient(hybridClientSecret, authCode){
        var clientData = {
            form: {
                "client_id": "func-test-hybrid",
                "client_secret": hybridClientSecret,
                "grant_type": "authorization_code",
                "redirect_uri": "http://localhost:5001",
                "code": authCode
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

        it("should be able to authenticate user using implicit flow", function(){           
            this.timeout(10000);           

            //setup custom phantomJS capability
            var phantomjsExe = require("phantomjs").path;
            var customPhantom = webdriver.Capabilities.phantomjs();
            customPhantom.set("phantomjs.binary.path", phantomjsExe);
            //build custom phantomJS driver
            var driver = new webdriver.Builder().
                withCapabilities(customPhantom).
                build();


            driver.manage().window().setSize(1024, 768);
            var encodedRedirectUri = encodeURIComponent(baseIdentityUrl);
            return driver.get(baseIdentityUrl + "/account/login?returnUrl=%2Fconnect%2Fauthorize%2Flogin%3Fclient_id%3Dfunc-test%26redirect_uri%3D" + encodedRedirectUri + "%26response_type%3Did_token%2520token%26scope%3Dopenid%2520profile%2520fabric%252Fauthorization.read%2520fabric%252Fauthorization.write%26nonce%3Dd9bfc7af239b4e99b18cb08f69f77377")
            .then(function(){  

                //sign in using driver
                driver.findElement(By.id("Username")).sendKeys("bob");
                driver.findElement(By.id("Password")).sendKeys("bob");
                driver.findElement(By.id("login_but")).click();                     
                return driver.getCurrentUrl();
            })
            .then(function(currentUrl){                   
                expect(currentUrl).to.include(baseIdentityUrl);
                
                var authUrl = url.parse(currentUrl);    
                var obj = qs.parse(authUrl.hash);
                var token = obj["access_token"];
                expect(token).to.not.be.undefined;               
                return Promise.resolve(token);
            })
            .then(function(accessToken){                
                //use access token to add a role 
                var options = {
                    headers: {
                        "content-type": "application/json",
                        "authorization": "Bearer " + accessToken
                    }
                }
                var roleFoo = {
                    "Grain": "app",
                    "SecurableItem": "func-test",
                    "Name": "FABRIC\\\Health Catalyst Viewer"
                }
                return chakram.post(baseAuthUrl + "/roles", roleFoo, options);    
            })
            .then(function(postResponse){
                expect(postResponse).to.have.status(201);
            });
        });

        it("should be able to authenticate user using hybrid flow", function(){
            this.timeout(10000);
             //setup custom phantomJS capability
            var phantomjsExe = require("phantomjs").path;
            var customPhantom = webdriver.Capabilities.phantomjs();
            customPhantom.set("phantomjs.binary.path", phantomjsExe);
            //build custom phantomJS driver
            var driver = new webdriver.Builder().
                withCapabilities(customPhantom).
                build();
            
            var hybridClientSecret = "";

            driver.manage().window().setSize(1024, 768);
            var encodedRedirectUri = encodeURIComponent(baseIdentityUrl);
            return chakram.post(baseIdentityUrl + "/api/client", identityClientHybridFuncTest, authRequestOptions)
            .then(function(postResponse){
                expect(postResponse).to.have.status(201);
                expect(postResponse).to.comprise.of.json({ clientId: "func-test-hybrid" });
                hybridClientSecret = postResponse.body.clientSecret;
                return driver.get(baseIdentityUrl + "/connect/authorize?client_id=func-test-hybrid&scope=openid profile offline_access fabric/identity.manageresources&response_type=code id_token&redirect_uri=http://localhost:5001&state=abx&nonce=xyz");
            })            
            .then(function(){  
                //sign in using driver
                driver.findElement(By.id("Username")).sendKeys("bob");
                driver.findElement(By.id("Password")).sendKeys("bob");
                driver.findElement(By.id("login_but")).click();                     
                return driver.getCurrentUrl();
            })            
            .then(function(currentUrl){                                   
                expect(currentUrl).to.include(baseIdentityUrl);
                
                var authUrl = url.parse(currentUrl);    
                var obj = qs.parse(authUrl.hash);
                var authCode = obj["#code"];
                expect(authCode).to.not.be.undefined;               
                return getAccessTokenForFuncTestHybridClient(hybridClientSecret, authCode);
            })
            .then(function(accessToken){                
                //use access token to get the client from identity
                var options = {
                    headers: {
                        "content-type": "application/json",
                        "authorization": accessToken
                    }
                }              
                return chakram.get(baseIdentityUrl + "/api/client/func-test-hybrid", options);    
            })
            .then(function(getResponse){
                expect(getResponse).to.have.status(200);
                expect(getResponse).to.comprise.of.json({ clientId: "func-test-hybrid" });
            });
        });
    });
});