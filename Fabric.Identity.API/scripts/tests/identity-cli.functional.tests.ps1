param(
    [string] $targetFilePath = "$PSScriptRoot\..\identity-cli.psm1",
    [Uri] $identityUrl = "http://localhost:5001"
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
    BeforeAll {
        # Set up auth/identity
        Set-Location "$targetFilePath/.."
        Start-Process "start-auth-identity.sh" -Wait
    }
    AfterAll {
        # Tear down auth/identity
        Start-Process "$targetFilePath\..\stop-auth-identity.sh" -Wait
    }
    
    Describe 'Set-Up' {        
        It 'Should return the correct issuer' {
            $url = ""
            [System.Uri]::TryCreate($identityUrl, ".well-known/openid-configuration", [ref]$url)

            $response = Invoke-RestMethod -Method Get -Uri $url
            $response.issuer | Should -Be "http://functional-identity:5001"
        }
    }

    Describe 'Get-AccessToken' {
        Context 'Valid Request' {
            # TODO
        }

        Context 'Invalid requests' {    
            It 'Should return the error when invalid request' {                
                try {
                    Get-AccessToken  -identityUrl $identityUrl -secret "Secret" -scope "scope" -clientId "id"
                }
                catch {
                    $error = Get-ErrorFromResponse -response $_.Exception.Response

                    $_.Exception | Should -BeOfType System.Net.WebException 
                    $error | Should -Be '{"error":"invalid_client"}'
                }
            }
        }
    }
    
    Describe 'Get-FabricInstallerAccessToken' {
        BeforeAll {
            # Register installer?
        }

        Context 'Valid Request' {
            # TODO
        }

        Context 'Invalid Requests' {
            It 'Should return an exception when invalid installer secret' {    
                try {
                    Get-FabricInstallerAccessToken  -identityUrl $identityUrl -secret "Secret" 
                }
                catch {
                    $error = Get-ErrorFromResponse -response $_.Exception.Response

                    $_.Exception | Should -BeOfType System.Net.WebException 
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