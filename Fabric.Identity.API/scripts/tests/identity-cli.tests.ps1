param(
    [string] $targetFilePath = "$PSScriptRoot\..\identity-cli.psm1"
)

# Force re-import to pick up latest changes
Import-Module $targetFilePath -Force

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

describe 'Get-ClientRegistration' {
    Context 'Unit Tests' {
        Context 'Valid Request' {
            It 'Should return a client object when valid response' {
                $mockUrl = New-MockObject -Type Uri
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
                $mockUrl = New-MockObject -Type Uri
                Mock -ModuleName identity-cli -CommandName Invoke-RestMethod { throw  New-Object -TypeName "System.Net.WebException" -ArgumentList "Exception" }
                
                {Get-ClientRegistration -identityUrl $mockUrl -clientId "someNonExistentClient" -accessToken  "Bearer goodtoken"} | Should -Throw "Exception" -ExceptionType System.Net.WebException
            }

            It 'Should return an error when bad token' {
                $mockUrl = New-MockObject -Type Uri
                Mock -ModuleName identity-cli -CommandName Invoke-RestMethod { throw  New-Object -TypeName "System.Net.WebException" -ArgumentList "Exception" }
                
                {Get-ClientRegistration -identityUrl $mockUrl -clientId "someClient" -accessToken  "Bearer badtoken"} | Should -Throw "Exception" -ExceptionType System.Net.WebException
            }
        }
    }
}


describe 'New-ClientRegistration' {}

describe 'New-ImplicitClientRegistration' {}

describe 'New-HybridClientRegistration' {}

describe 'New-HybridPkceClientRegistration' {}

describe 'Invoke-UpdateClientRegistration' {}

describe 'Invoke-UpdateClientPassword' {}

describe 'Test-IsClientRegistered' {}

describe 'Get-ApiRegistration' {}

describe 'New-ApiRegistration' {}

describe 'Invoke-UpdateApiRegistration' {}

describe 'Invoke-UpdateClientPassword' {}

describe 'Test-IsApiRegistered' {}
