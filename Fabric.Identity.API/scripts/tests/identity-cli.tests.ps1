param(
    [string] $targetFilePath = "$PSScriptRoot\..\identity-cli.psm1"
)

# Force re-import to pick up latest changes
Import-Module $targetFilePath -Force

Describe 'Get-AccessToken' {
    Context 'Unit Tests' {
        Context 'Valid Request' {
            BeforeAll {
                Mock -ModuleName identity-cli -CommandName Invoke-RestMethod { return @{
                    access_token = "return token"
                    expires_in = 3600
                    token_type =  "Bearer"
                    }
                }
                [Uri] $mockUrl = "http://identity"
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
                [Uri] $mockUrl = "http://identity"
                
                {Get-AccessToken -identityUrl $mockUrl -clientId "id" -secret "secret" -scope "fabric/identity.manageresources"} | Should -Throw "Exception" -ExceptionType System.Net.WebException
            }
        }
    }
}

Describe 'Get-FabricInstallerAccessToken' {
    Context 'Unit Tests' {
        Context 'Valid Request' {
            It 'Should return an access token when valid response' {
                [Uri] $mockUrl = "http://identity"
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
                [Uri] $mockUrl = "http://identity"

                try {
                    Get-FabricInstallerAccessToken  -identityUrl $mockUrl -secret "Secret" 
                }
                catch {
                    $_.Exception | Should -BeOfType System.Net.WebException 
                    $_.Exception.Message | Should -Be "Error"
                }
            }
        }
    }
}

Describe 'Get-ClientRegistration' {
    Context 'Unit Tests' {
        Context 'Valid Request' {
            It 'Should return a client object when valid response' {
                [Uri] $mockUrl = "http://identity"
                Mock -ModuleName identity-cli -CommandName Invoke-RestMethod { return @{
                    enabled = 1
                    clientId = "dos-metadata-service"
                    allowedGrantTypes = @("client_credentials", "delegation")
                    allowedScopes = @("fabric/authorization.read", "dos/metadata", "fabric.profile")                                        
                }
                }

                $response = Get-ClientRegistration -identityUrl $mockUrl -clientId "someClient" -accessToken  "Bearer goodtoken"                

                $response.clientId | Should -Be "dos-metadata-service"
                $response.allowedScopes.Length | Should -Be 3
            }
        }

        Context 'Invalid request' {    
            It 'Should return an error when non existent client' {
                [Uri] $mockUrl = "http://identity"
                Mock -ModuleName identity-cli -CommandName Invoke-RestMethod { throw  New-Object -TypeName "System.Net.WebException" -ArgumentList "Exception" }
                
                {Get-ClientRegistration -identityUrl $mockUrl -clientId "someNonExistentClient" -accessToken  "Bearer goodtoken"} | Should -Throw "Exception" -ExceptionType System.Net.WebException
            }

            It 'Should return an error when bad token' {
                [Uri] $mockUrl = "http://identity"
                Mock -ModuleName identity-cli -CommandName Invoke-RestMethod { throw  New-Object -TypeName "System.Net.WebException" -ArgumentList "Exception" }
                
                {Get-ClientRegistration -identityUrl $mockUrl -clientId "someClient" -accessToken  "Bearer badtoken"} | Should -Throw "Exception" -ExceptionType System.Net.WebException
            }
        }
    }
}


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
