param(
    [string] $targetFilePath = "$PSScriptRoot\..\CatalystDosIdentity.psm1"
)

# Force re-import to pick up latest changes
Import-Module $targetFilePath -Force

Describe 'Get-AccessToken Unit Tests' -tag 'Unit' {
    Context 'Valid Request' {
        BeforeAll {
            Mock -ModuleName CatalystDosIdentity -CommandName Invoke-RestMethod { return @{
                    access_token = "return token"
                    expires_in   = 3600
                    token_type   = "Bearer"
                }
            }
			[Uri] $mockUrl = "http://identity"
        }

        It 'Should return an access token when valid response with single scope'{
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
            Mock -ModuleName CatalystDosIdentity -CommandName Invoke-RestMethod { throw  New-Object -TypeName "System.Net.WebException" -ArgumentList "Exception" }
			[Uri] $mockUrl = "http://identity"

            {Get-AccessToken -identityUrl $mockUrl -clientId "id" -secret "secret" -scope "fabric/identity.manageresources"} | Should -Throw "Exception" -ExceptionType System.Net.WebException
        }
    }
}

Describe 'Get-FabricInstallerAccessToken Unit Tests' -tag 'Unit' {
    Context 'Valid Request' {
        It 'Should return an access token when valid response' {
			[Uri] $mockUrl = "http://identity"
            Mock -ModuleName CatalystDosIdentity -CommandName Invoke-RestMethod { return @{
                    access_token = "return token"
                    expires_in   = 3600
                    token_type   = "Bearer"
                }
            }

            $response = Get-FabricInstallerAccessToken -identityUrl $mockUrl -secret "Secret"

            $response | Should -Be "return token"
        }
    }

    Context 'Invalid Request' {
        It 'Should return an exception when invalid response' {
            Mock -ModuleName CatalystDosIdentity -CommandName Invoke-RestMethod { throw  New-Object -TypeName "System.Net.WebException" -ArgumentList "Error" }
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

Describe 'Get-ClientRegistration Unit Tests' -tag 'Unit' {
    Context 'Valid Request' {
        It 'Should return a client object when valid response' {
			[Uri] $mockUrl = "http://identity"
            Mock -ModuleName CatalystDosIdentity -CommandName Invoke-RestMethod { return @{
                    enabled           = 1
                    clientId          = "dos-metadata-service"
                    allowedGrantTypes = @("client_credentials", "delegation")
                    allowedScopes     = @("fabric/authorization.read", "dos/metadata", "fabric.profile")
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
            Mock -ModuleName CatalystDosIdentity -CommandName Invoke-RestMethod { throw  New-Object -TypeName "System.Net.WebException" -ArgumentList "Exception" }
            {Get-ClientRegistration -identityUrl $mockUrl -clientId "someNonExistentClient" -accessToken  "Bearer goodtoken"} | Should -Throw "Exception" -ExceptionType System.Net.WebException
        }

        It 'Should return an error when bad token' {
			[Uri] $mockUrl = "http://identity"
            Mock -ModuleName CatalystDosIdentity -CommandName Invoke-RestMethod { throw  New-Object -TypeName "System.Net.WebException" -ArgumentList "Exception" }

            {Get-ClientRegistration -identityUrl $mockUrl -clientId "someClient" -accessToken  "Bearer badtoken"} | Should -Throw "Exception" -ExceptionType System.Net.WebException
        }
    }
}

Describe 'New-ClientRegistrationTests Unit Tests' -tag 'Unit' {
    Context 'Valid Request - new client' {
        It 'New Registration should return a client secret when valid new client' {
            $authUrl = [System.Uri]'https://some.Server/identity'

            $newClient = New-ClientCredentialsClientBody `
                -clientId "cliTestClient" `
                -clientName "cli Test Client Name" `
                -allowedScopes @("fabric/identity.manageresources", "fabric/authorization.read", "fabric/authorization.write", "fabric/authorization.manageclients")

            $jsonClient = $newClient | ConvertTo-Json

            Mock -ModuleName CatalystDosIdentity -CommandName Invoke-RestMethod { return @{
                    enabled      = 1
                    clientSecret = "newClientSecret"
                }} `
                -ParameterFilter {
                $uri -match "$authUrl"
            }

            $response = New-ClientRegistration -identityUrl $authUrl -body $jsonClient -accessToken "goodToken"

            $response | Should -Be "newClientSecret"
        }
    }

    Context 'Valid Request - existing client' {
        It 'New Registration should return a new client secret when client already exists' {
            $authUrl = [System.Uri]'https://some.Server/identity'

            $newClient = New-ClientCredentialsClientBody `
                -clientId "cliTestClient" `
                -clientName "cli Test Client Name" `
                -allowedScopes @("fabric/identity.manageresources", "fabric/authorization.read", "fabric/authorization.write", "fabric/authorization.manageclients")

            $jsonClient = $newClient | ConvertTo-Json

            Mock -ModuleName CatalystDosIdentity -CommandName Assert-WebExceptionType -Verifiable -MockWith {
                return $true
            } -ParameterFilter {$typeCode -eq 409}

            Mock -ModuleName CatalystDosIdentity -CommandName Invoke-RestMethod -Verifiable -MockWith {
                throw  New-Object -TypeName "System.Net.WebException" -ArgumentList "conflict"
            } -ParameterFilter {$Method -match 'Post' -and $uri -match "$authUrl/api/client"}

            Mock -ModuleName CatalystDosIdentity -CommandName Invoke-RestMethod -Verifiable -MockWith {
                return ""
            } -ParameterFilter { $Method -match 'Put' -and $uri -match "$authUrl/api/client/$($newClient.clientId)"}

            Mock -ModuleName CatalystDosIdentity -CommandName Invoke-RestMethod { return @{
                    enabled      = 1
                    clientSecret = "someClientSecret"
                }} -ParameterFilter {$Method -match 'Post' -and $uri -match "$authUrl/api/client/$($newClient.clientId)/resetPassword"}

            $response = New-ClientRegistration -identityUrl $authUrl -body $jsonClient -accessToken "good token"

            $response | Should -Be "someClientSecret"
        }
    }

    Context 'Invalid Request - existing client, invalid json' {
        It 'New Registration should throw an exception when client already exists, and new json is invalid' {
            $authUrl = [System.Uri]'https://some.Server/identity2'

            $newClient = New-ClientCredentialsClientBody `
                -clientId "cliTestClient" `
                -clientName "cli Test Client Name" `
                -allowedScopes @("fabric/identity.manageresources", "fabric/authorization.read", "fabric/authorization.write", "fabric/authorization.manageclients")

            $jsonClient = $newClient | ConvertTo-Json

            Mock -ModuleName CatalystDosIdentity -CommandName Assert-WebExceptionType -Verifiable -MockWith {
                return $true
            } -ParameterFilter {$typeCode -eq 409}

            Mock -ModuleName CatalystDosIdentity -CommandName Invoke-RestMethod -Verifiable -MockWith {
                throw  New-Object -TypeName "System.Net.WebException" -ArgumentList "conflict"
            } -ParameterFilter {$Method -match 'Post' -and $uri -match "$authUrl/api/client"}

            Mock -ModuleName CatalystDosIdentity -CommandName Invoke-RestMethod -Verifiable -MockWith {
                throw  New-Object -TypeName "System.Net.WebException" -ArgumentList "put error"
            } -ParameterFilter { $Method -match 'Put' -and $uri -match "$authUrl/api/client/$($newClient.clientId)"}

            {New-ClientRegistration -identityUrl $authUrl -body $jsonClient -accessToken "goodToken"} | Should -Throw "There was an error updating Client" -ExceptionType System.Net.WebException
        }
    }

    Context 'Invalid request - invalid json' {
        It 'New Registration should return an error when invalid client json' {
            $authUrl = [System.Uri]'https://some.Server/identity'

            $newClient = @{}
            $newClient.Add("clientId", "cliTestClient")
            $newClient.Add("clientName", "cli Test Client Name")
            $newClient.Add("requireConsent", "false")

            $jsonClient = $newClient | ConvertTo-Json

            Mock -ModuleName CatalystDosIdentity -CommandName Invoke-RestMethod {
                throw  New-Object -TypeName "System.Net.WebException" -ArgumentList "There was an error registering client"
            } `
                -ParameterFilter {
                $uri -match "$authUrl"
            }
            {New-ClientRegistration -identityUrl $authUrl -body $jsonClient -accessToken "goodToken"} | Should -Throw "There was an error registering client" -ExceptionType System.Net.WebException
        }
    }
    Context "Invalid request - bad token" {
        It 'new registration should return an error when bad token' {
            $authUrl = [System.Uri]'https://some.Server/identity'

            $newClient = New-ClientCredentialsClientBody `
                -clientId "cliTestClient" `
                -clientName "cli Test Client Name" `
                -allowedScopes @("fabric/identity.manageresources", "fabric/authorization.read", "fabric/authorization.write", "fabric/authorization.manageclients")

            $jsonClient = $newClient | ConvertTo-Json

            Mock -ModuleName CatalystDosIdentity -CommandName Assert-WebExceptionType -Verifiable -MockWith {
                return $false
            } -ParameterFilter {$typeCode -eq 409}

            Mock -ModuleName CatalystDosIdentity -CommandName Invoke-RestMethod -Verifiable -MockWith {
                throw  New-Object -TypeName "System.Net.WebException" -ArgumentList "(401) Unauthorized"
            } -ParameterFilter {$Method -match 'Post' -and $uri -match "$authUrl/api/client"}

            {New-ClientRegistration -identityUrl $authUrl -body $jsonClient -accessToken "badToken"} | Should -Throw "There was an error registering client" -ExceptionType System.Net.WebException
        }
    }
}

Describe 'New-ClientCredentialsClientBody Unit Tests' -tag 'Unit' {
    Context 'Valid Object' {
        It 'New-ClientCredentialsClientBody should return a valid object with required parameters' {
            $myClientId = "validClientCredentialsClient"
            $myClientName = "Valid client credentials client name"

            $response = New-ClientCredentialsClientBody -clientId $myClientId -clientName $myClientName -allowedScopes @("fabric/authorization.read", "dos/metadata", "fabric.profile")
            $response | Should -Not -Be :null
            $response.ClientId | Should -Be $myClientId

            #check default values
            $response.allowedGrantTypes | Should -Be "client_credentials"
            $response.allowedScopes | Should -Contain "fabric/authorization.read"
            $response.allowedScopes.Count | Should -Be 3
        }
    }
}

Describe 'New-ImplicitClientBody Unit Tests' -tag 'Unit' {
    Context 'Valid Object' {
        It 'New-ImplicitClientBody should return a valid object with required parameters' {
            $myClientId = "validImplicitClient"
            $myClientName = "Valid implicit client name"

            $response = New-ImplicitClientBody -clientId $myClientId -clientName $myClientName -allowedScopes @("fabric/authorization.read", "dos/metadata", "fabric.profile")
            $response | Should -Not -Be :null
            $response.ClientId | Should -Be $myClientId

            #check default values
            $response.allowedGrantTypes | Should -Be "implicit"
            $response.allowedScopes | Should -Contain "dos/metadata"
            $response.allowOfflineAccess | Should -Be $false
        }
    }
}

Describe 'New-HybridClientBody Unit Tests' -tag 'Unit' {
    Context 'Valid Object' {
        It 'New-HybridClientBody should return a valid object with required parameters' {
            $myClientId = "validHybridClient"
            $myClientName = "Valid hybrid client name"

            $response = New-HybridClientBody -clientId $myClientId -clientName $myClientName -allowedScopes @("fabric/authorization.read", "dos/metadata", "fabric.profile")
            $response | Should -Not -Be :null
            $response.ClientId | Should -Be $myClientId

            #check default values
            $response.allowedGrantTypes | Should -Be "hybrid"
            $response.allowedScopes | Should -Contain "dos/metadata"
            $response.allowOfflineAccess | Should -Be $true
        }
    }
}

Describe 'New-HybridPkceClientBody Unit Tests' -tag 'Unit' {
    Context 'Valid Object' {
        It 'New-HybridPkceClientBody should return a valid object with required parameters' {
            $myClientId = "validHybridPkceClient"
            $myClientName = "Valid Hybrid PKCE client name"

            $response = New-HybridPkceClientBody -clientId $myClientId -clientName $myClientName -allowedScopes @("fabric/authorization.read", "dos/metadata", "fabric.profile")
            $response | Should -Not -Be :null
            $response.ClientId | Should -Be $myClientId

            #check default values
            $response.allowedGrantTypes | Should -Be "hybrid"
            $response.allowedScopes | Should -Contain "dos/metadata"
            $response.updateAccessTokenClaimsOnRefresh | Should -Be $true
        }
    }
}

Describe 'Edit-ClientRegistration Unit Tests' -tag 'Unit' {
    Context 'Valid Request' {
        It 'Edit-ClientRegistration should return nothing on successful update' {
            $authUrl = [System.Uri]'https://some.Server/identity'

            $newClient = New-ClientCredentialsClientBody `
                -clientId "cliTestClient" `
                -clientName "NEW cli Test Client Name" `
                -allowedScopes @("fabric/identity.manageresources", "fabric/authorization.read", "fabric/authorization.write", "fabric/authorization.manageclients")

            $jsonClient = $newClient | ConvertTo-Json

            Mock -ModuleName CatalystDosIdentity -CommandName Invoke-RestMethod { return ''} `
                -ParameterFilter {
                $uri -match "$authUrl"
            }

            $response = Edit-ClientRegistration -identityUrl $authUrl -body $jsonClient -accessToken "good token"

            $response | Should -Be ""
        }
    }

    Context 'Invalid request - bad json' {
        It 'Edit-ClientRegistration should return an error when invalid client json' {
            $authUrl = [System.Uri]'https://some.Server/identity'

            $newClient = @{}
            $newClient.Add("clientId", "cliTestClientssss")
            $newClient.Add("clientName", "cli Test Client Name")
            $newClient.Add("requireConsent", "false")

            $jsonClient = $newClient | ConvertTo-Json

            Mock -ModuleName CatalystDosIdentity -CommandName Invoke-RestMethod {
                throw  New-Object -TypeName "System.Net.WebException" -ArgumentList "There was an error registering client"
            } `
                -ParameterFilter {
                $uri -match "$authUrl"
            }

            {Edit-ClientRegistration -identityUrl $authUrl -body $jsonClient -accessToken "goodToken"} | Should -Throw "There was an error updating client registration" -ExceptionType System.Net.WebException
        }
    }
    Context 'Invalid request - bad token' {
        It 'Edit-ClientRegistration should return an error when bad token' {
            $authUrl = [System.Uri]'https://some.Server/identity'

            $newClient = @{}
            $newClient.Add("clientId", "cliTestClientssss")
            $newClient.Add("clientName", "new cli Test Client Name")
            $newClient.Add("requireConsent", "false")

            $jsonClient = $newClient | ConvertTo-Json

            Mock -ModuleName CatalystDosIdentity -CommandName Invoke-RestMethod {
                throw  New-Object -TypeName "System.Net.WebException" -ArgumentList "Unauthorized"
            } `
                -ParameterFilter {
                $uri -match "$authUrl"
            }

            {Edit-ClientRegistration -identityUrl $authUrl  -body $jsonClient -accessToken  "Bearer badtoken"} | Should -Throw "There was an error updating client registration" -ExceptionType System.Net.WebException
        }
    }
}

Describe 'Reset-ClientPassword Unit Tests' -tag 'Unit' {
    Context 'Valid Request' {
        It 'Reset-ClientPassword should return client string on successful reset' {
            $authUrl = [System.Uri]'https://some.Server/identity'

            $myToken = "good token"

            Mock -ModuleName CatalystDosIdentity -CommandName Invoke-RestMethod { return @{
                enabled      = 1
                clientSecret = "someClientSecret"
            }} -ParameterFilter {$Method -match 'Post' -and $uri -match "$authUrl/api/client/cliTestClient/resetPassword"}

            $response = Reset-ClientPassword -identityUrl $authUrl -clientId "cliTestClient"  -accessToken $myToken
            $response | Should -Not -Be :null
            $response | Should -Not -Be ""
            $response.Length | Should -BeGreaterThan 8
        }
    }

    Context 'Invalid Request - Not Found' {
        It 'Reset-ClientPassword should throw and error if client not found' {
            $authUrl = [System.Uri]'https://some.Server/identity'

            $myToken = "good token"

            Mock -ModuleName CatalystDosIdentity -CommandName Invoke-RestMethod {
                throw  New-Object -TypeName "System.Net.WebException" -ArgumentList "There was an error registering client"
            } -ParameterFilter {$uri -match "$authUrl"}

            {Reset-ClientPassword -identityUrl $authUrl -clientId "missing client id" -accessToken $myToken} | Should -Throw "There was an error resetting client secret" -ExceptionType System.Net.WebException
        }
    }
}

Describe 'Test-IsClientRegistered Unit Tests' -tag 'Unit' {
    Context 'Valid Request -- Client found' {
        It 'Test-IsClientRegistered should return true if the client id IS found' {
            $authUrl = [System.Uri]'https://some.Server/identity'

            $myToken = 'good token'

            Mock -ModuleName CatalystDosIdentity -CommandName Invoke-RestMethod { return @{
                    enabled           = 1
                    clientId          = "cliTestClient"
                    allowedGrantTypes = @("client_credentials", "delegation")
                    allowedScopes     = @("fabric/authorization.read", "dos/metadata", "fabric.profile")
                }
            }

            $response = Test-IsClientRegistered -identityUrl $authUrl -clientId "cliTestClient"  -accessToken $myToken
            $response | Should -Not -Be :null
            $response | Should -Be $true
        }
    }
    Context 'Invalid Request -- Client not found' {
        It 'Test-IsClientRegistered should return false if the client id is not found' {
            $authUrl = [System.Uri]'https://some.Server/identity'

            $myToken = 'good token'

            Mock -ModuleName CatalystDosIdentity -CommandName Assert-WebExceptionType -Verifiable -MockWith {
                return $true
            } -ParameterFilter {$typeCode -eq 404}

            Mock -ModuleName CatalystDosIdentity -CommandName Invoke-RestMethod -Verifiable -MockWith {
                throw  New-Object -TypeName "System.Net.WebException" -ArgumentList "not found"
            } -ParameterFilter {$Method -match 'Get' -and $uri -match "$authUrl/api/client"}

            $response = Test-IsClientRegistered -identityUrl $authUrl -clientId "missing client"  -accessToken $myToken
            $response | Should -Not -Be :null
            $response | Should -Be $false
        }
    }
}

Describe 'Get-ApiRegistration Unit Tests' -tag "Unit" {
    Context 'Valid Request' {
        It 'Should Return an api object when valid response' {
            [Uri] $mockUrl = "http://some.server/identity"
            Mock -ModuleName CatalystDosIdentity -CommandName Invoke-RestMethod { return @{
                enabled = 1
                name = "test-Api"
                scopes = @{"name" = "test-Api"; "displayName" = "Test-API"}
                userClaims = @("name", "email", "role", "groups")
            }}

            $response = Get-ApiRegistration -identityUrl $mockUrl -apiName "someApi" -accessToken  "Bearer goodtoken"

            $response.name | Should -Be "test-Api"
            $response.userClaims.Length | Should -Be 4
            $response.scopes.Count | Should -Be 2
        }}
    Context 'Invalid Request' {
        It 'Should return an error when non existent api' {
            [Uri] $mockUrl = "http://some.server/identity"
            Mock -ModuleName CatalystDosIdentity -CommandName Invoke-RestMethod { throw  New-Object -TypeName "System.Net.WebException" -ArgumentList "Exception" }

            {Get-ApiRegistration -identityUrl $mockUrl -apiName "someNonExistentApi" -accessToken  "Bearer goodtoken"} | Should -Throw "Exception" -ExceptionType System.Net.WebException
        }
    }
}

Describe 'New-ApiRegistration Unit Tests' -tag "Unit" {
    Context 'Valid Request' {
        It 'Should Create and Return an api secret when valid response' {
            [Uri] $mockUrl = "http://some.server/identity"

            $newApiResource = New-ApiRegistrationBody `
                                -apiName "test-Api" `
                                -scopes @{"name" = "test-Api"; "displayName" = "Test-API"} `
                                -userClaims @("name", "email", "role", "groups") `
                                -isEnabled true

            $jsonApi = $newApiResource | ConvertTo-Json

            Mock -ModuleName CatalystDosIdentity -CommandName Invoke-RestMethod { return @{
                apiSecret = "someApiSecret"
                enabled = 1
                name = "test-Api"
                scopes = @{"name" = "test-Api"; "displayName" = "Test-API"}
                userClaims = @("name", "email", "role", "groups")
            }}

            $response = New-ApiRegistration -identityUrl $mockUrl -body $jsonApi -accessToken  "goodtoken"

            $response | Should -Be "someApiSecret"
        }}
    Context 'Valid Request - existing api' {
        It 'New Registration should return a new api secret when api already exists' {
            [Uri] $mockUrl = "http://some.server/identity"

            $newApiResource = New-ApiRegistrationBody `
                                -apiName "test-Api" `
                                -scopes @{"name" = "test-Api"; "displayName" = "Test-API"} `
                                -userClaims @("name", "email", "role", "groups") `
                                -isEnabled true

            $jsonApi = $newApiResource | ConvertTo-Json

            Mock -ModuleName CatalystDosIdentity -CommandName Assert-WebExceptionType -Verifiable -MockWith {
                return $true
            } -ParameterFilter {$typeCode -eq 409}

            Mock -ModuleName CatalystDosIdentity -CommandName Invoke-RestMethod -Verifiable -MockWith {
                throw  New-Object -TypeName "System.Net.WebException" -ArgumentList "conflict"
            } -ParameterFilter {$Method -match 'Post' -and $uri -match "$mockUrl/api/apiresource"}

            Mock -ModuleName CatalystDosIdentity -CommandName Invoke-RestMethod -Verifiable -MockWith {
                return ""
            } -ParameterFilter { $Method -match 'Put' -and $uri -match "$mockUrl/api/apiresource/$($newApiResource.name)"}

            Mock -ModuleName CatalystDosIdentity -CommandName Reset-ApiPassword { return @{
                    apiSecret = "someApiSecret"
                    enabled = 1
                    name = "test-Api"
                    scopes = @{"name" = "test-Api"; "displayName" = "Test-API"}
                    userClaims = @("name", "email", "role", "groups")
                }}

            $response = New-ApiRegistration -identityUrl $mockUrl -body $jsonApi -accessToken  "goodtoken"

            $response.apiSecret | Should -Be "someApiSecret"
        }
    }
    Context 'Invalid Request - invalid json' {
        It 'Should return an error when json is invalid' {
            [Uri] $mockUrl = "http://some.server/identity"
            $newApiResource = New-ApiRegistrationBody `
            -apiName "test-Api" `
            -scopes @{"name" = "test-Api"; "displayName" = "Test-API"} `
            -userClaims @("name", "email", "role", "groups") `
            -isEnabled true

            $newApiResource.scopes = ""

            $jsonApi = $newApiResource | ConvertTo-Json

            Mock -ModuleName CatalystDosIdentity -CommandName Invoke-RestMethod { throw  New-Object -TypeName "System.Net.WebException" -ArgumentList "There was an error registering api" } `
            -ParameterFilter {$Method -match 'Post' -and $uri -match "$mockUrl/api/apiresource"}

            {New-ApiRegistration -identityUrl $mockUrl -body $jsonApi -accessToken  "goodtoken"} | Should -Throw "There was an error registering api" -ExceptionType System.Net.WebException
        }
    }
}

Describe 'Remove-ApiRegistration Unit Tests' -tag 'Unit' {
    Context 'Valid Request' {
        It 'Remove Registration should return no content' {
            [Uri] $mockUrl = "http://some.server/identity"
            $newApiResource = New-ApiRegistrationBody `
            -apiName "test-Api" `
            -scopes @{"name" = "test-Api"; "displayName" = "Test-API"} `
            -userClaims @("name", "email", "role", "groups") `
            -isEnabled true

            Mock -ModuleName CatalystDosIdentity -CommandName Invoke-RestMethod -Verifiable -MockWith {
                return ""
            } -ParameterFilter { $Method -match 'Delete' -and $uri -match "$mockUrl/api/apiresource/$($newApiResource.name)"}

            $response = Remove-ApiRegistration -identityUrl $mockUrl -apiName $($newApiResource.name) -accessToken  "goodtoken"

            $response | Should -Be ""
        }
    }
    Context 'Invalid Request - Not Found' {
        It 'Should return an error when not found' {
            [Uri] $mockUrl = "http://some.server/identity"
            $newApiResource = New-ApiRegistrationBody `
            -apiName "test-Api" `
            -scopes @{"name" = "test-Api"; "displayName" = "Test-API"} `
            -userClaims @("name", "email", "role", "groups") `
            -isEnabled true

            Mock -ModuleName CatalystDosIdentity -CommandName Invoke-RestMethod { throw  New-Object -TypeName "System.Net.WebException" -ArgumentList "There was an error deleting api" } `
            -ParameterFilter {$uri -match "$mockUrl"}

            {Remove-ApiRegistration -identityUrl $mockUrl -apiName $($newApiResource.name) -accessToken  "goodtoken"} | Should -Throw "There was an error deleting api" -ExceptionType System.Net.WebException
        }
    }
}

Describe 'Edit-ApiRegistration Unit Tests' -tag "Unit" {
    Context 'Valid Request - existing api' {
        It 'Edit Registration should return a new api secret when api already exists' {
            [Uri] $mockUrl = "http://some.server/identity"

            $newApiResource = New-ApiRegistrationBody `
                              -apiName "test-Api" `
                              -scopes @{"name" = "test-Api"; "displayName" = "Test-API"} `
                              -userClaims @("name", "email", "role", "groups") `
                              -isEnabled true

            $jsonApi = $newApiResource | ConvertTo-Json

            Mock -ModuleName CatalystDosIdentity -CommandName Invoke-RestMethod -Verifiable -MockWith {
                return ""
            } -ParameterFilter { $Method -match 'Put' -and $uri -match "$mockUrl/api/apiresource/$($newApiResource.name)"}

            Mock -ModuleName CatalystDosIdentity -CommandName Reset-ApiPassword { return @{
                apiSecret = "someApiSecret"
                enabled = 1
                name = "test-Api"
                scopes = @{"name" = "test-Api"; "displayName" = "Test-API"}
                userClaims = @("name", "email", "role", "groups")
            }}

            $response = Edit-ApiRegistration -identityUrl $mockUrl -body $jsonApi -apiName $($newApiResource.name) -accessToken  "goodtoken"

            $response.apiSecret | Should -Be "someApiSecret"
        }
    }
    Context 'Invalid Request - invalid json' {
        It 'Should return an error when json is invalid' {
            [Uri] $mockUrl = "http://some.server/identity"
            $newApiResource = New-ApiRegistrationBody `
            -apiName "test-Api" `
            -scopes @{"name" = "test-Api"; "displayName" = "Test-API"} `
            -userClaims @("name", "email", "role", "groups") `
            -isEnabled true

            $newApiResource.scopes = ""

            $jsonApi = $newApiResource | ConvertTo-Json

            Mock -ModuleName CatalystDosIdentity -CommandName Invoke-RestMethod { throw  New-Object -TypeName "System.Net.WebException" -ArgumentList "There was an error editing api" } `
            -ParameterFilter { $Method -match 'Put' -and $uri -match "$mockUrl/api/apiresource/$($newApiResource.name)"}

            {Edit-ApiRegistration -identityUrl $mockUrl -body $jsonApi -apiName $($newApiResource.name) -accessToken  "goodtoken"} | Should -Throw "There was an error editing api" -ExceptionType System.Net.WebException
        }
    }
}

Describe 'Reset-ApiPassword Unit Tests' -tag 'Unit' {
    Context 'Valid Request' {
        It 'Resetting password should return api secret' {
            [Uri] $mockUrl = "http://some.server/identity"
            $newApiResource = New-ApiRegistrationBody `
            -apiName "test-Api" `
            -scopes @{"name" = "test-Api"; "displayName" = "Test-API"} `
            -userClaims @("name", "email", "role", "groups") `
            -isEnabled true

            Mock -ModuleName CatalystDosIdentity -CommandName Invoke-RestMethod { return @{
                apiSecret = "someApiSecret"
                enabled = 1
                name = "test-Api"
                scopes = @{"name" = "test-Api"; "displayName" = "Test-API"}
                userClaims = @("name", "email", "role", "groups")
            }} -ParameterFilter {$Method -match 'Post' -and $uri -match "$mockUrl/api/apiresource/$($newApiResource.name)/resetPassword"}

            $response = Reset-ApiPassword -identityUrl $mockUrl -apiName $($newApiResource.name) -accessToken  "goodtoken"

            $response | Should -Be "someApiSecret"
        }
    }
    Context 'Invalid Request - Not Found' {
        It 'Should return an error when not found' {
            [Uri] $mockUrl = "http://some.server/identity"
            $newApiResource = New-ApiRegistrationBody `
            -apiName "test-Api" `
            -scopes @{"name" = "test-Api"; "displayName" = "Test-API"} `
            -userClaims @("name", "email", "role", "groups") `
            -isEnabled true

            Mock -ModuleName CatalystDosIdentity -CommandName Invoke-RestMethod { throw  New-Object -TypeName "System.Net.WebException" -ArgumentList "There was an error resetting api password" } `
            -ParameterFilter {$Method -match 'Post' -and $uri -match "$mockUrl/api/apiresource/$($newApiResource.name)/resetPassword"}

            {Reset-ApiPassword -identityUrl $mockUrl -apiName $($newApiResource.name) -accessToken  "goodtoken"} | Should -Throw "There was an error resetting api password" -ExceptionType System.Net.WebException
        }
    }
}

Describe 'Test-IsApiRegistered Unit Tests' -tag "Unit" {
    Context 'Valid Request' {
        It 'Should return true if an api object exists' {
            [Uri] $mockUrl = "http://some.server/identity"
            $newApiResource = New-ApiRegistrationBody `
            -apiName "test-Api" `
            -scopes @{"name" = "test-Api"; "displayName" = "Test-API"} `
            -userClaims @("name", "email", "role", "groups") `
            -isEnabled true

            Mock -ModuleName CatalystDosIdentity -CommandName Invoke-RestMethod { return @{
                enabled = 1
                name = "test-Api"
                scopes = @{"name" = "test-Api"; "displayName" = "Test-API"}
                userClaims = @("name", "email", "role", "groups")
            }} -ParameterFilter {$Method -match 'Get' -and $uri -match "$mockUrl/api/apiresource/$($newApiResource.name)"}

            $response = Test-IsApiRegistered -identityUrl $mockUrl -apiName $($newApiResource.name) -accessToken  "goodtoken"

            $response | Should -Be $true
        }
    }
    Context 'Invalid Request - Not Found' {
        It 'Should return false when not found' {
            [Uri] $mockUrl = "http://some.server/identity"
            $newApiResource = New-ApiRegistrationBody `
            -apiName "test-Api" `
            -scopes @{"name" = "test-Api"; "displayName" = "Test-API"} `
            -userClaims @("name", "email", "role", "groups") `
            -isEnabled true

            Mock -ModuleName CatalystDosIdentity -CommandName Assert-WebExceptionType -Verifiable -MockWith {
                return $true
            } -ParameterFilter {$typeCode -eq 404}

            Mock -ModuleName CatalystDosIdentity -CommandName Invoke-RestMethod -Verifiable -MockWith {
                throw  New-Object -TypeName "System.Net.WebException" -ArgumentList "not found"
            } -ParameterFilter {$Method -match 'Get' -and $uri -match "$mockUrl/api/apiresource/$($newApiResource.name)"}

            $response = Test-IsApiRegistered -identityUrl $mockUrl -apiName 'api not found' -accessToken  "goodtoken"

            $response | Should -Be $false
        }
    }
}




