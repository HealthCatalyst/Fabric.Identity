param(
    [string] $targetFilePath = "$PSScriptRoot\..\identity-cli.psm1",
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
    
    Describe 'Get-ApiRegistration' {}
    
    Describe 'New-ApiRegistration' {}
    
    Describe 'Invoke-UpdateApiRegistration' {}
    
    Describe 'Invoke-UpdateClientPassword' {}
    
    Describe 'Test-IsApiRegistered' {}

}