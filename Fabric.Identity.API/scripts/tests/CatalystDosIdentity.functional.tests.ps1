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

            It 'Should return an access token when request does not have a scope' {
                $response = Get-AccessToken -identityUrl $identityUrl -clientId "fabric-installer" -secret $installerSecret
                $response | Should -Not -Be $null
            }
        }

        Context 'Invalid requests' {
            It 'Should return an error when request has an invalid client id' {
                try {
                    Get-AccessToken  -clientId "id" -identityUrl $identityUrl -secret "Secret" -scope "scope"
                }
                catch {
                    $error = Get-ErrorFromResponse -response $_.Exception.Response

                    $_.Exception | Should -BeOfType System.Net.WebException 
                    $error | Should -Be '{"error":"invalid_client"}'
                }
            }

            It 'Should return an exception when request does not have a valid secret' {
                try {
                    $response = Get-AccessToken -identityUrl $identityUrl -clientId "fabric-installer" -secret "secret" -scope "fabric/identity.manageresources"
                }
                catch {
                    $_.Exception | Should -BeOfType System.Net.WebException
                    $error = Get-ErrorFromResponse -response $_.Exception.Response

                    $error | Should -Be '{"error":"invalid_client"}'
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
                }
                catch {
                    $_.Exception | Should -BeOfType System.Net.WebException 
                    
                    $error = Get-ErrorFromResponse -response $_.Exception.Response
                    $error | Should -Be '{"error":"invalid_client"}'
                }
            }
        }
    }
    Describe 'Get-ClientRegistration' {}
        
    Describe 'New-ClientRegistration' {}
    
    Describe 'New-ImplicitClientRegistration' {}
    
    Describe 'New-HybridClientRegistration' {}
    
    Describe 'New-HybridPkceClientRegistration' {}
    
    Describe 'Invoke-UpdateClientRegistration' {}
    
    Describe 'Invoke-UpdateClientPassword' {}
    
    Describe 'Test-IsClientRegistered' {}

    Describe 'Api Registration Functional Tests' -tag "Functional" {
	    Context 'Api Registration Scenarios' {
            	# Get Access token from identity to pass to registration
                $accessToken = Get-AccessToken -identityUrl $identityUrl -clientId "fabric-installer" -secret $installerSecret -scope "fabric/identity.manageresources"

                # Get-FabricInstallerAccessToken returns an access token if request succeeds, expect a value when successful
                $accessToken | Should -Not -Be $null

                # New-ApiRegistrationBody returns an apiresource object
                $newApiResource = New-ApiRegistrationBody `
                -apiName "test-Api" `
                -scopes @{"name" = "test-Api"; "displayName" = "Test-API"} `
                -userClaims @("name", "email", "role", "groups") `
                -isEnabled true

                $jsonApi = $newApiResource | ConvertTo-Json

            It 'Should not fail to Test, Create, Test, Get, Edit and Delete Registration' {

                # Test an api not registered
                $testApi = Test-IsApiRegistered -identityUrl $identityUrl -apiName "test-Api" -accessToken $accessToken

                $testApi | Should -Be $false

                # Create an api not registered
                $newApi = New-ApiRegistration -identityUrl $identityUrl -body $jsonApi -accessToken $accessToken

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

                $jsonApi = $newApiResource | ConvertTo-Json

                $editApi = Edit-ApiRegistration -identityUrl $identityUrl -body $jsonApi -apiName "test-Api" -accessToken $accessToken

                $editApi | Should -Not -Be $null

                # Cleanup the registered Api
                $removeApi = Remove-ApiRegistration -identityUrl $identityUrl -apiName "test-Api" -accessToken $accessToken

                $removeApi | Should -Be ""

                # Getting the api that was soft deleted should return not found
                try{
                   Get-ApiRegistration -identityUrl $identityUrl -apiName "test-Api" -accessToken $accessToken
                }
                catch {
                   $_.Exception | Should -BeOfType System.Net.WebException

                   $error = Get-ErrorFromResponse -response $_.Exception.Response
                   $error | Should Match "not found"
                }
            }
            It 'Should not fail using New-ApiRegistration for an api already registered' {

                # Create the Api
                $newApi = New-ApiRegistration -identityUrl $identityUrl -body $jsonApi -accessToken $accessToken

                $newApi | Should -Not -Be $null

                # Creating the Api again will end up as an edit instead of a conflict
                $editApi = New-ApiRegistration -identityUrl $identityUrl -body $jsonApi -accessToken $accessToken

                $editApi | Should -Not -Be $null
                $editApi | Should -Not -Be $newApi

                # Cleanup the registered Api
                $removeApi = Remove-ApiRegistration -identityUrl $identityUrl -apiName "test-Api" -accessToken $accessToken

                $removeApi | Should -Be ""
            }
            It 'Should fail to Edit Registration using url api name different from json body api name' {

                # Create the Api
                $newApi = New-ApiRegistration -identityUrl $identityUrl -body $jsonApi -accessToken $accessToken

                $newApi | Should -Not -Be $null

                # Editing using a different url api name than in the json body results in an error
                try {
                    Edit-ApiRegistration -identityUrl $identityUrl -body $jsonApi -apiName "sample-Api" -accessToken $accessToken
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
                   }
                   catch {
                       $_.Exception | Should -BeOfType System.Net.WebException

                       $error = Get-ErrorFromResponse -response $_.Exception.Response
                       $error | Should Match "not found"
                }

                # Error trying to Edit an Api not registered
                try {
                    Edit-ApiRegistration -identityUrl $identityUrl -body $jsonApi -apiName "test-Api" -accessToken $accessToken
                   }
                   catch {
                       $_.Exception | Should -BeOfType System.Net.WebException

                       $error = Get-ErrorFromResponse -response $_.Exception.InnerException.Response
                       $error | Should Match "not found"
                }

                # Error trying to Remove an Api not registered
                try {
                    Remove-ApiRegistration -identityUrl $identityUrl -apiName "test-Api" -accessToken $accessToken
                   }
                   catch {
                       $_.Exception | Should -BeOfType System.Net.WebException

                       $error = Get-ErrorFromResponse -response $_.Exception.InnerException.Response
                       $error | Should Match "not found"
                }

                # Error trying to Reset a password for an api not registered
                try {
                    Reset-ApiPassword -identityUrl $identityUrl -apiName "test-Api" -accessToken $accessToken
                   }
                   catch {
                       $_.Exception | Should -BeOfType System.Net.WebException

                       $error = Get-ErrorFromResponse -response $_.Exception.InnerException.Response
                       $error | Should Match "not found"
                }
            }
        }
	}
}