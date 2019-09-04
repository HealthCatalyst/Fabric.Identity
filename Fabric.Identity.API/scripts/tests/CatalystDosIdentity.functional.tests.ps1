param(
    [string] $targetFilePath = "$PSScriptRoot\..\CatalystDosIdentity.psm1",
    [Uri] $identityUrl = "http://localhost:5001",
    [string] $installerSecret
)

# Force re-import to pick up latest changes
Import-Module $targetFilePath -Force

function Get-ErrorFromResponse($response) {
    $result = $response.GetResponseStream()
    $reader = New-Object System.IO.StreamReader($result)
    $reader.BaseStream.Position = 0
    $reader.DiscardBufferedData()
    $responseBody = $reader.ReadToEnd();
    return $responseBody
}

Describe 'Identity Cli Functional Tests' {
    Describe 'Get-AccessToken' {
        Context 'Valid Request' {
            It 'Should return an access token when valid request' {
                $response = Get-AccessToken -identityUrl $identityUrl -clientId "fabric-installer" -secret $installerSecret -scope "fabric/identity.manageresources"

                # Get-FabricInstallerAccessToken returns an access token if request succeeds, expect a value when successful
                $response | Should -Not -Be $null
            }

            It 'Should return an access token when valid request and multiple valid scopes' {
                $response = Get-AccessToken -identityUrl $identityUrl -clientId "fabric-installer" -secret $installerSecret -scope "fabric/identity.manageresources fabric/authorization.read fabric/authorization.write fabric/authorization.dos.write fabric/authorization.manageclients"

                # Get-FabricInstallerAccessToken returns an access token if request succeeds, expect a value when successful
                $response | Should -Not -Be $null
            }
        }

        Context 'Invalid requests' {
            It 'Should return an error when request has an invalid client id' {
                try {
                    Get-AccessToken  -clientId "id" -identityUrl $identityUrl -secret $installerSecret -scope "fabric/identity.manageresources"
                    Throw "Should fail with an invalid clientId"
                }
                catch {
                    $error = Get-ErrorFromResponse -response $_.Exception.Response

                    $_.Exception | Should -BeOfType System.Net.WebException 
                    $error | Should -Be '{"error":"invalid_client"}'
                }
            }

            It 'Should return an exception when request does not have a valid secret' {
                try {
                    Get-AccessToken -identityUrl $identityUrl -clientId "fabric-installer" -secret "secret" -scope "fabric/identity.manageresources"
                    Throw "Should fail with an invalid secret"
                }
                catch {
                    $_.Exception | Should -BeOfType System.Net.WebException
                    $error = Get-ErrorFromResponse -response $_.Exception.Response

                    $error | Should -Be '{"error":"invalid_client"}'
                }
            }

            It 'Should return an exception when the request does not have a scope' {
                try {
                    Get-AccessToken -identityUrl $identityUrl -clientId "fabric-installer" -secret $installerSecret
                    Throw "Should fail without a scope"
                }
                catch {
                    $_.Exception | Should -BeOfType System.Net.WebException
                    $error = Get-ErrorFromResponse -response $_.Exception.Response

                    $error | Should -Be '{"error":"invalid_scope"}'
                }
            }
        }
    }

    Describe 'Get-FabricInstallerAccessToken' {
        Context 'Valid Request' {
            It 'Should return an access token when valid request' {
                $response = Get-FabricInstallerAccessToken -identityUrl $identityUrl -secret $installerSecret

                # Get-FabricInstallerAccessToken returns an access token if request succeeds, expect a value when successful
                $response | Should -Not -Be $null
            }
        }

        Context 'Invalid Requests' {
            It 'Should return an exception when invalid installer secret' {
                try {
                    Get-FabricInstallerAccessToken  -identityUrl $identityUrl -secret "Secret" 
                    Throw "Should fail with an invalid installer secret"
                }
                catch {
                    $_.Exception | Should -BeOfType System.Net.WebException 
                    
                    $error = Get-ErrorFromResponse -response $_.Exception.Response
                    $error | Should -Be '{"error":"invalid_client"}'
                }
            }
        }
    }

    Describe 'Client Flows' {
        Context 'Happy Client Lifecycle' {
            # variable defintions            
            $timeString = (Get-Date).ToString("hhmmss")
            $testClientId = "CredentialsHappy$timeString"
            $fabricToken = Get-FabricInstallerAccessToken -identityUrl $identityUrl -secret $installerSecret

            $newClient = New-ClientCredentialsClientBody `
                -clientId $testClientId `
                -clientName "Name for $testClientId" `
                -allowedScopes @("fabric/identity.manageresources", "fabric/authorization.read", "fabric/authorization.write", "fabric/authorization.manageclients")

            $originalSecret = ""
            $resetSecret = ""

            It 'Setup and ensure client is not already registered'{
                $clientExists = Test-IsClientRegistered -identityUrl $identityUrl -clientId $testClientId -accessToken $fabricToken
                $clientExists | Should -Be $false
            }
            It 'Generate and register a new client object' {
                $jsonClient = $newClient | ConvertTo-Json
                $originalSecret = New-ClientRegistration -identityUrl $identityUrl -body $jsonClient -accessToken $fabricToken

                $originalSecret | Should -Not -Be null
            } 
            It 'Edit the client to include new scopes' { 
                $newClient["clientName"] = "new Name"
                $newClient["allowedGrantTypes"] = @("client_credentials", "delegation")
                $jsonClient = $newClient | ConvertTo-Json

                $response = Edit-ClientRegistration -identityUrl $identityUrl -body $jsonClient -accessToken $fabricToken

                $response | Should -Be ""
            }
            It 'Get the updated client and verify update' {
                $updateClient = Get-ClientRegistration -identityUrl $identityUrl -clientId $testClientId -accessToken $fabricToken

                $updateClient | Should -Not -Be null

                $updateClient.clientName | Should -Be "new Name"
                $updateClient.allowedGrantTypes.Count | Should -Be 2
            }
            It 'Reset the client secret' {
                $resetSecret = Reset-ClientPassword -identityUrl $identityUrl -clientId $testClientId -accessToken $fabricToken
                $resetSecret | Should -Not -Be null
                $resetSecret | Should -Not -Be $originalSecret
            }
            It 'Call New-ClientRegistration again to Upsert the original scopes' {
                $newClient["clientName"] = "original name"
                $upsertSecret = New-ClientRegistration -identityUrl $identityUrl -body $newClient -accessToken $fabricToken

                $upsertSecret | Should -Not -Be null
                $upsertSecret | Should -Not -Be $resetSecret
            }
            It 'Test to make sure the client exists - positive test for IsClientRegistered' {
                $clientExists = Test-IsClientRegistered -identityUrl $identityUrl -clientId $testClientId -accessToken $fabricToken

                $clientExists | Should -Be $true
            }
        }

        Context 'Happy Create Client Flows' {
            It 'Creates new Client Credentials client' {
                $timeString = (Get-Date).ToString("hhmmss")
                $fabricToken = Get-FabricInstallerAccessToken -identityUrl $identityUrl -secret $installerSecret
                $credentialsId = "ClientCredentialsClientBody$timeString"

                # New-ClientCredentialsClientBody (expect object)
                $newClient = New-ClientCredentialsClientBody `
                    -clientId $credentialsId `
                    -clientName "Name for $credentialsId" `
                    -allowedScopes @("fabric/identity.manageresources", "fabric/authorization.read", "fabric/authorization.write", "fabric/authorization.manageclients")

                # Client Credentials New-ClientRegistration (expect client secret)
                $newResults = New-ClientRegistration -identityUrl $identityUrl -body $newClient -accessToken $fabricToken
                $newResults | Should -Not -Be null

                # Get Client (expect a client credentials )
                $checkClient = Get-ClientRegistration -identityUrl $identityUrl -clientId $credentialsId -accessToken $fabricToken

                $checkClient | Should -Not -Be null

                $checkClient.clientName | Should -Be "Name for $credentialsId"
                $checkClient.allowedGrantTypes.Count | Should -Be 1
                $checkClient.allowedGrantTypes[0] | Should -Be "client_credentials"
            }

            It 'Creates new Hybrid client' {
                $timeString = (Get-Date).ToString("hhmmss")
                $fabricToken = Get-FabricInstallerAccessToken -identityUrl $identityUrl -secret $installerSecret
                $hybridId = "HybridClientBody$timeString"

                # New-ClientCredentialsClientBody (expect object)
                $newClient = New-HybridClientBody `
                    -clientId $hybridId `
                    -clientName "Name for $hybridId" `
                    -allowedScopes @("fabric/authorization.read", "fabric.profile")

                # Client Credentials New-ClientRegistration (expect client secret)
                $jsonClient = $newClient | ConvertTo-Json
                $newResults = New-ClientRegistration -identityUrl $identityUrl -body $jsonClient -accessToken $fabricToken
                $newResults | Should -Not -Be null

                # Get Client (expect a client credentials )
                $checkClient = Get-ClientRegistration -identityUrl $identityUrl -clientId $hybridId -accessToken $fabricToken

                $checkClient | Should -Not -Be null

                $checkClient.clientName | Should -Be "Name for $hybridId"
                $checkClient.allowedGrantTypes.Count | Should -Be 1
                $checkClient.allowedGrantTypes[0] | Should -Be "hybrid"
            }

            It 'Creates new HybridPkceClientBody client' {
                $timeString = (Get-Date).ToString("hhmmss")
                $fabricToken = Get-FabricInstallerAccessToken -identityUrl $identityUrl -secret $installerSecret
                $hybridPkceId = "HybridPkceClientBody$timeString"

                # New-ClientCredentialsClientBody (expect object)
                $newClient = New-HybridPkceClientBody `
                    -clientId $hybridPkceId `
                    -clientName "Name for $hybridPkceId" `
                    -allowedScopes @("fabric/authorization.read", "fabric.profile")

                # Client Credentials New-ClientRegistration (expect client secret)
                $jsonClient = $newClient | ConvertTo-Json
                $newResults = New-ClientRegistration -identityUrl $identityUrl -body $jsonClient -accessToken $fabricToken
                $newResults | Should -Not -Be null

                # Get Client (expect a client credentials )
                $checkClient = Get-ClientRegistration -identityUrl $identityUrl -clientId $hybridPkceId -accessToken $fabricToken

                $checkClient | Should -Not -Be null

                $checkClient.clientName | Should -Be "Name for $hybridPkceId"
                $checkClient.allowedGrantTypes.Count | Should -Be 1
                $checkClient.allowedGrantTypes[0] | Should -Be "hybrid"
            }

            It 'Creates new Implicit client' {
                $timeString = (Get-Date).ToString("hhmmss")
                $fabricToken = Get-FabricInstallerAccessToken -identityUrl $identityUrl -secret $installerSecret
                $implicitId = "ImplicitClientBody$timeString"

                # New-ClientCredentialsClientBody (expect object)
                $newClient = New-ImplicitClientBody `
                    -clientId $implicitId `
                    -clientName "Name for $implicitId" `
                    -allowedScopes @("fabric/identity.manageresources", "fabric/authorization.read", "fabric/authorization.write", "fabric/authorization.manageclients") `
                    -allowedCorsOrigins @("127.0.0.1")

                # Client Credentials New-ClientRegistration (expect client secret)
                $jsonClient = $newClient | ConvertTo-Json
                $newResults = New-ClientRegistration -identityUrl $identityUrl -body $jsonClient -accessToken $fabricToken
                $newResults | Should -Not -Be null

                # Get Client (expect a client credentials )
                $checkClient = Get-ClientRegistration -identityUrl $identityUrl -clientId $implicitId -accessToken $fabricToken

                $checkClient | Should -Not -Be null

                $checkClient.clientName | Should -Be "Name for $implicitId"
                $checkClient.allowedGrantTypes.Count | Should -Be 1
                $checkClient.allowedGrantTypes[0] | Should -Be "implicit"
            }
        }

        Context 'Get-ClientRegistration issues' {
            It 'Get-ClientRegistration requires a token' {
                $timeString = (Get-Date).ToString("hhmmss")
                $fabricToken = "sorry charlie"
                $clientId = "anyClient$timeString"

                try {
                    Get-ClientRegistration -identityUrl $identityUrl -clientId $clientId -accessToken $fabricToken
                    Throw "Should fail with an invalid token"
                }
                catch {
                    $_.Exception | Should -BeOfType System.Net.WebException
                    $unauthorizedError = Assert-WebExceptionType $_.Exception 401

                    $unauthorizedError | Should -BeTrue
                }
            }

            It 'Get-ClientRegistration client not found' {
                $timeString = (Get-Date).ToString("hhmmss")
                $fabricToken = Get-FabricInstallerAccessToken -identityUrl $identityUrl -secret $installerSecret
                $credentialsId = "notFoundClient$timeString"

                try {
                    Get-ClientRegistration -identityUrl $identityUrl -clientId $credentialsId -accessToken $fabricToken
                    Throw "Should fail with an invalid clientId"
                }
                catch {
                    $_.Exception | Should -BeOfType System.Net.WebException
                    $notfoundError = Assert-WebExceptionType $_.Exception 404

                    $notfoundError | Should -BeTrue
                }
            }
        }

        Context 'New-ClientRegistration issues' {
            It 'New-ClientRegistration requires a token' {
                $timeString = (Get-Date).ToString("hhmmss")
                $fabricToken = "sorry charlie"
                $credentialsId = "ClientCredentialsClientBody$timeString"

                # New-ClientCredentialsClientBody (expect object)
                $newClient = New-ClientCredentialsClientBody `
                    -clientId $credentialsId `
                    -clientName "Name for $credentialsId" `
                    -allowedScopes @("fabric/identity.manageresources", "fabric/authorization.read", "fabric/authorization.write", "fabric/authorization.manageclients")

                # Client Credentials New-ClientRegistration (expect client secret)
                $jsonClient = $newClient | ConvertTo-Json

                try {
                    New-ClientRegistration -identityUrl $identityUrl -body $jsonClient -accessToken $fabricToken
                    Throw "Should fail with an invalid token"
                }
                catch {
                    $_.Exception | Should -BeOfType System.Net.WebException
                    $error = $_.Exception.InnerException
                    $unauthorizedError = Assert-WebExceptionType $_.Exception.InnerException 401

                    $unauthorizedError | Should -BeTrue
                }
            }

            It 'New-ClientRegistration requires allowedScopes' {
                $timeString = (Get-Date).ToString("hhmmss")
                $fabricToken = Get-FabricInstallerAccessToken -identityUrl $identityUrl -secret $installerSecret
                $credentialsId = "ClientCredentialsClientBody$timeString"

                # New-ClientCredentialsClientBody (expect object)
                $newClient = New-ClientCredentialsClientBody `
                    -clientId $credentialsId `
                    -clientName "Name for $credentialsId" `
                    -allowedScopes @("fabric/identity.manageresources", "fabric/authorization.read", "fabric/authorization.write", "fabric/authorization.manageclients")

                $newClient.Remove("allowedScopes")

                # Client Credentials New-ClientRegistration (expect client secret)
                $jsonClient = $newClient | ConvertTo-Json

                try {
                    New-ClientRegistration -identityUrl $identityUrl -body $jsonClient -accessToken $fabricToken
                    Throw "Should fail without allowedScopes"
                }
                catch {
                    $_.Exception | Should -BeOfType System.Net.WebException
                    $error = Get-ErrorFromResponse -response $_.Exception.InnerException.Response

                    $error | Should -BeLike "*Please specify at least one Allowed Scope*"
                }
            }
        }

        Context 'Edit-ClientRegistration issues' {
            It 'Edit-ClientRegistration requires a token' {
                $timeString = (Get-Date).ToString("hhmmss")
                $fabricToken = "sorry charlie"
                $clientId = "anyClient$timeString"

                $newClient = New-ImplicitClientBody `
                    -clientId $clientId `
                    -clientName "Name for $clientId" `
                    -allowedScopes @("fabric/identity.manageresources", "fabric/authorization.read", "fabric/authorization.write", "fabric/authorization.manageclients") `
                    -allowedCorsOrigins @("127.0.0.1")

                $jsonClient = $newClient | ConvertTo-Json

                try {
                    Edit-ClientRegistration -identityUrl $identityUrl -body $jsonClient -accessToken $fabricToken
                    Throw "Should fail with an invalid token"
                }
                catch {
                    $_.Exception | Should -BeOfType System.Net.WebException
                    $error = $_.Exception.InnerException
                    $unauthorizedError = Assert-WebExceptionType $_.Exception.InnerException 401

                    $unauthorizedError | Should -BeTrue
                }
            }

            It 'Edit-ClientRegistration invalid body' {
                $timeString = (Get-Date).ToString("hhmmss")
                $fabricToken = Get-FabricInstallerAccessToken -identityUrl $identityUrl -secret $installerSecret
                $clientId = "notFoundClient$timeString"

                $newClient = New-ImplicitClientBody `
                    -clientId $clientId `
                    -clientName "Name for $clientId" `
                    -allowedScopes @("fabric/identity.manageresources", "fabric/authorization.read", "fabric/authorization.write", "fabric/authorization.manageclients") `
                    -allowedCorsOrigins @("127.0.0.1")

                $jsonClient = $newClient | ConvertTo-Json

                try {
                    Edit-ClientRegistration -identityUrl $identityUrl -body $jsonClient -accessToken $fabricToken
                    Throw "Should fail with an invalid body"
                }
                catch {
                    $_.Exception | Should -BeOfType System.Net.WebException
                    $notfoundError = Assert-WebExceptionType $_.Exception.InnerException 404

                    $notfoundError | Should -BeTrue
                }
            }
        }

        Context 'Reset-ClientPassword issues' {
            It 'Reset-ClientPassword requires a token' {
                $timeString = (Get-Date).ToString("hhmmss")
                $fabricToken = "sorry charlie"
                $clientId = "anyClient$timeString"

                try {
                    Reset-ClientPassword -identityUrl $identityUrl -clientId $clientId -accessToken $fabricToken
                    Throw "Should fail when passing an invalid token"
                }
                catch {
                    $_.Exception | Should -BeOfType System.Net.WebException
                    $error = $_.Exception.InnerException
                    $unauthorizedError = Assert-WebExceptionType $_.Exception.InnerException 401

                    $unauthorizedError | Should -BeTrue
                }
            }

            It 'Reset-ClientPassword not found' {
                $timeString = (Get-Date).ToString("hhmmss")
                $fabricToken = Get-FabricInstallerAccessToken -identityUrl $identityUrl -secret $installerSecret
                $clientId = "notFoundClient$timeString"

                try {
                    Reset-ClientPassword -identityUrl $identityUrl -clientId $clientId -accessToken $fabricToken
                    Throw "Should fail when clientId not found"
                }
                catch {
                    $_.Exception | Should -BeOfType System.Net.WebException
                    $notfoundError = Assert-WebExceptionType $_.Exception.InnerException 404

                    $notfoundError | Should -BeTrue
                }
            }
        }
    }

    Describe 'Api Registration Functional Tests' -tag "Functional" {
        Context 'Api Registration Scenarios' {

            # Get Access token from identity to pass to registration
            $accessToken = Get-AccessToken -identityUrl $identityUrl -clientId "fabric-installer" -secret $installerSecret -scope "fabric/identity.manageresources"

            # Get-FabricInstallerAccessToken returns an access token if request succeeds, expect a value when successful
            $accessToken | Should -Not -Be $null

            # New-ApiRegistrationBody returns an apiresource object
            $newApiResource = New-ApiRegistrationBody `
                -apiName "test-Api" `
                -scopes  @{"name" = "test-Api"; "displayName" = "Test-API"} `
                -userClaims @("name", "email", "role", "groups") `
                -isEnabled true


            It 'Should not fail to Test, Create, Test, Get, Edit and Delete Registration' {

                # Test an api not registered
                $testApi = Test-IsApiRegistered -identityUrl $identityUrl -apiName "test-Api" -accessToken $accessToken

                $testApi | Should -Be $false

                # Create an api not registered
                $newApi = New-ApiRegistration -identityUrl $identityUrl -body $newApiResource -accessToken $accessToken

                $newApi | Should -Not -Be $null

                # Test an api that is registered
                $testApi = Test-IsApiRegistered -identityUrl $identityUrl -apiName "test-Api" -accessToken $accessToken

                $testApi | Should -Be $true

                # Get an api that is registered
                $getApi = Get-ApiRegistration -identityUrl $identityUrl -apiName "test-Api" -accessToken $accessToken

                $getApi | Should -Not -Be $null
                $getApi.name | Should -Be "test-Api"

                # Edit an api that is registered
                $newApiResource = New-ApiRegistrationBody `
                    -apiName "test-Api" `
                    -scopes @{"name" = "patient-Api"; "displayName" = "Patient-API"} `
                    -userClaims @("name", "email", "role", "groups") `
                    -isEnabled true

                $editApi = Edit-ApiRegistration -identityUrl $identityUrl -body $newApiResource -apiName "test-Api" -accessToken $accessToken

                $editApi | Should -Not -Be $null

                # Cleanup the registered Api
                $removeApi = Remove-ApiRegistration -identityUrl $identityUrl -apiName "test-Api" -accessToken $accessToken

                $removeApi | Should -Be ""

                # Getting the api that was logically deleted should return not found
                try {
                    Get-ApiRegistration -identityUrl $identityUrl -apiName "test-Api" -accessToken $accessToken
                    Throw "Should get not found for an api that was logically deleted"
                }
                catch {
                    $_.Exception | Should -BeOfType System.Net.WebException

                    $error = Get-ErrorFromResponse -response $_.Exception.Response
                    $error | Should Match "not found"
                }
            }
            It 'Should not fail using New-ApiRegistration for an api already registered' {

                # Create the Api
                $newApi = New-ApiRegistration -identityUrl $identityUrl -body $newApiResource -accessToken $accessToken

                $newApi | Should -Not -Be $null

                # Creating the Api again will end up as an edit instead of a conflict
                $editApi = New-ApiRegistration -identityUrl $identityUrl -body $newApiResource -accessToken $accessToken

                $editApi | Should -Not -Be $null
                $editApi | Should -Not -Be $newApi

                # Cleanup the registered Api
                $removeApi = Remove-ApiRegistration -identityUrl $identityUrl -apiName "test-Api" -accessToken $accessToken

                $removeApi | Should -Be ""
            }
            It 'Should fail to Edit Registration using url api name different from json body api name' {

                # Create the Api
                $newApi = New-ApiRegistration -identityUrl $identityUrl -body $newApiResource -accessToken $accessToken

                $newApi | Should -Not -Be $null

                # Editing using a different url api name than in the json body results in an error
                try {
                    Edit-ApiRegistration -identityUrl $identityUrl -body $newApiResource -apiName "sample-Api" -accessToken $accessToken
                    Throw "Should be an error using a different api name than in the json body"
                }
                catch {
                    $_.Exception | Should -BeOfType System.Net.WebException

                    $error = Get-ErrorFromResponse -response $_.Exception.InnerException.Response
                    $error | Should Match "must match the ApiResource Name in the request body"
                }

                # Cleanup the registered Api
                $removeApi = Remove-ApiRegistration -identityUrl $identityUrl -apiName "test-Api" -accessToken $accessToken

                $removeApi | Should -Be ""
            }
            It 'Should fail to Get, Edit and Delete Registration and Reset Password for an api not registered' {

                # Error trying to Get an Api not registered
                try {
                    Get-ApiRegistration -identityUrl $identityUrl -apiName "test-Api" -accessToken $accessToken
                    Throw "Should be an error trying to get an api not registered"
                }
                catch {
                    $_.Exception | Should -BeOfType System.Net.WebException

                    $error = Get-ErrorFromResponse -response $_.Exception.Response
                    $error | Should Match "not found"
                }

                # Error trying to Edit an Api not registered
                try {
                    Edit-ApiRegistration -identityUrl $identityUrl -body $newApiResource -apiName "test-Api" -accessToken $accessToken
                    Throw "Should be an error trying to edit an api not registered"
                }
                catch {
                    $_.Exception | Should -BeOfType System.Net.WebException

                    $error = Get-ErrorFromResponse -response $_.Exception.InnerException.Response
                    $error | Should Match "not found"
                }

                # Error trying to Remove an Api not registered
                try {
                    Remove-ApiRegistration -identityUrl $identityUrl -apiName "test-Api" -accessToken $accessToken
                    Throw "Should be an error trying to remove an api not registered"
                }
                catch {
                    $_.Exception | Should -BeOfType System.Net.WebException

                    $error = Get-ErrorFromResponse -response $_.Exception.InnerException.Response
                    $error | Should Match "not found"
                }

                # Error trying to Reset a password for an api not registered
                try {
                    Reset-ApiPassword -identityUrl $identityUrl -apiName "test-Api" -accessToken $accessToken
                    Throw "Should have failed to reset a password for an api not registered"
                }
                catch {
                    $_.Exception | Should -BeOfType System.Net.WebException

                    $error = Get-ErrorFromResponse -response $_.Exception.InnerException.Response
                    $error | Should Match "not found"
                }
            }
            It 'Should fail using New-ApiRegistration without a scope' {

                # Remove scopes to test registration without a scope
                $newApiResource.Remove("scopes")

                try{
                  # Create the Api
                  New-ApiRegistration -identityUrl $identityUrl -body $newApiResource -accessToken $accessToken
                  Throw "Should have failed using New-ApiRegistration without a scope"
                }
                catch{
                    $_.Exception | Should -BeOfType System.Net.WebException
                    $error = $_.Exception.Message.ToString()
                    $error.Contains("'Scopes' must not be empty.")
                }
            }
        }
    }
}