# param(
#     [string] $targetFilePath = "$PSScriptRoot\..\identity-cli.psm1"
# )

# Force re-import to pick up latest changes
Import-Module ..\identity-cli.psm1 -Force
#Import-Module $targetFilePath -Force

describe 'Get-AccessToken' {
    Context 'Unit Tests' {
        Context 'Valid Request' {
            BeforeAll {
                Mock -ModuleName identity-cli -CommandName Invoke-RestMethod { return @{
                    access_token = "return token"
                    expires_in = 3600
                    token_type =  "Bearer"
                    }
                }      
                $mockUrl = New-MockObject -Type Uri
            }

            It 'Should return an access token when valid response with single scope' {          
                $response = Get-AccessToken -identityUrl $mockUrl -clientId "id" -secret "secret" -scope "fabric/identity.manageresources"
                $response | Should -Be "return token"
            }

            It 'Should return an access token when valid response and no scopes' {
                $response = Get-AccessToken -identityUrl $mockUrl -clientId "id" -secret "secret"
                $response | Should -Be "return token"
            }
        }

        Context 'Invalid request' {    
            It 'Should return the error when invalid response' {
                Mock -ModuleName identity-cli -CommandName Invoke-RestMethod { throw  New-Object -TypeName "System.Net.WebException" -ArgumentList "Exception" }
                
                {Get-AccessToken -identityUrl "url" -clientId "id" -secret "secret" -scope "fabric/identity.manageresources"} | Should -Throw "Exception" -ExceptionType System.Net.WebException
            }
        }
    }
}

describe 'Get-FabricInstallerAccessToken' {
    Context 'Unit Tests' {
        Context 'Valid Request' {
            It 'Should return an access token when valid response' {
                $mockUrl = New-MockObject -Type Uri
                Mock -ModuleName identity-cli -CommandName Invoke-RestMethod { return @{
                    access_token = "return token"
                    expires_in = 3600
                    token_type =  "Bearer"
                    }
                }

                $response = Get-FabricInstallerAccessToken -identityUrl $mockUrl -secret "Secret"

                $response | Should -Be "return token"
            }
        }

        Context 'Invalid Request' {
            It 'Should return an exception when invalid response' {
                Mock -ModuleName identity-cli -CommandName Invoke-RestMethod { throw  New-Object -TypeName "System.Net.WebException" -ArgumentList "Error" }

                try {
                    Get-FabricInstallerAccessToken  -identityUrl "url" -secret "Secret" 
                }
                catch {
                    $_.Exception | Should -BeOfType System.Net.WebException 
                    $_.Exception.Message | Should -Be "Error"
                }
            }
        }
    }
}