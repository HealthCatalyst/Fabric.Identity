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


describe -Name 'New-ClientRegistrationTests' {
    Context 'Unit Tests' {
        Context 'Valid Request - new client' {
            It 'Should return a client secret when valid new client' {
                $authUrl = [System.Uri]'https://some.Server/identity'
                
                $newClient = New-ClientCredentialsClientBody `
                        -clientId "cliTestClient" `
                        -clientName "cli Test Client Name" `
                        -allowedScopes @("fabric/identity.manageresources", "fabric/authorization.read", "fabric/authorization.write", "fabric/authorization.manageclients")

                $jsonClient = $newClient | ConvertTo-Json

                Mock -ModuleName identity-cli -CommandName Invoke-RestMethod { return @{
                    enabled = 1
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
            It 'Should return a new client secret when client already exists' {
                $authUrl = [System.Uri]'https://some.Server/identity'
                
                $newClient = New-ClientCredentialsClientBody `
                        -clientId "cliTestClient" `
                        -clientName "cli Test Client Name" `
                        -allowedScopes @("fabric/identity.manageresources", "fabric/authorization.read", "fabric/authorization.write", "fabric/authorization.manageclients")

                $jsonClient = $newClient | ConvertTo-Json

                Mock -CommandName Invoke-RestMethod -Verifiable -MockWith {  
                    throw  New-Object -TypeName "System.Net.WebException" -ArgumentList '{"code":"Conflict","message":"Client cliTestClient already exists. Please provide a new client id","target":"Client","details":[{"code":"PredicateValidator","message":"Client cliTestClient already exists. Please provide a new client id","target":"ClientId","details":null,"innererror":null}],"innererror":null}' 
                } `
                -ParameterFilter {
                    $uri -match "$authUrl/api/client"
                    $Method -match 'Post'
                }  -ModuleName identity-cli

                $response = New-ClientRegistration -identityUrl $authUrl -body $jsonClient -accessToken "good token"

                Write-Warning "raw response=|$response|"
            
                $response | Should -Be "newClientSecret"
            }
        }
    }
    Context 'Invalid request' {    
        It 'Should return an error when invalid client json' {
            $authUrl = [System.Uri]'https://some.Server/identity'

            $newClient = @{}                                                                                                                                                    
            $newClient.Add("clientId", "cliTestClient")                                                                                                                         
            $newClient.Add("clientName", "cli Test Client Name")                                                                                                                
            $newClient.Add("requireConsent", "false")                                                                                                                           

            $jsonClient = $newClient | ConvertTo-Json

            Mock -ModuleName identity-cli -CommandName Invoke-RestMethod {
                throw  New-Object -TypeName "System.Net.WebException" -ArgumentList "There was an error registering client" 
            } `
            -ParameterFilter {
                $uri -match "$authUrl"
            } 
            
            {New-ClientRegistration -identityUrl $authUrl -body $jsonClient -accessToken "goodToken"} | Should -Throw "There was an error registering client" -ExceptionType System.Net.WebException
        }

        It 'Should return an error when bad token' {
            $authUrl = [System.Uri]'https://some.Server/identity'
            Mock -ModuleName identity-cli -CommandName Invoke-RestMethod {
                throw  New-Object -TypeName "System.Net.WebException" -ArgumentList "Exception" 
            } `
            -ParameterFilter {
                $uri -match "$authUrl"
            } 
            
            {Get-ClientRegistration -identityUrl $authUrl -clientId "someClient" -accessToken  "Bearer badtoken"} | Should -Throw "Exception" -ExceptionType System.Net.WebException
        }
    }
}

describe 'New-ClientCredentialsClientBody' {
    Context 'Unit Tests' {
        Context 'Valid Object' {
            It 'Should return a valid object with required parameters' {          
                $myClientId = "validClientCredentialsClient"
                $myClientName = "Valid client credentials client name"

                $response = New-ClientCredentialsClientBody -clientId $myClientId -clientName $myClientName -allowedScopes @("fabric/authorization.read", "dos/metadata", "fabric.profile") 
                $response | Should -not -Be :null
                $response.ClientId | Should -Be $myClientId

                #check default values
                $response.allowedGrantTypes | Should -Be "client_credentials"
                $response.allowedScopes | Should -Contain "fabric/authorization.read"
                $response.allowedScopes.Count | Should -be 3
            }
        }
    }
}

describe 'New-ImplicitClientBody' {
    Context 'Unit Tests' {
        Context 'Valid Object' {
            It 'Should return a valid object with required parameters' {          
                $myClientId = "validImplicitClient"
                $myClientName = "Valid implicit client name"

                $response = New-ImplicitClientBody -clientId $myClientId -clientName $myClientName -allowedScopes @("fabric/authorization.read", "dos/metadata", "fabric.profile") 
                $response | Should -not -Be :null
                $response.ClientId | Should -Be $myClientId

                #check default values
                $response.allowedGrantTypes | Should -Be "implicit"
                $response.allowedScopes | Should -Contain "dos/metadata"
                $response.allowOfflineAccess | Should -Be $false
            }
        }
    }
}

describe 'New-HybridClientBody' {
    Context 'Unit Tests' {
        Context 'Valid Object' {
            It 'Should return a valid object with required parameters' {          
                $myClientId = "validHybridClient"
                $myClientName = "Valid hybrid client name"

                $response = New-HybridClientBody -clientId $myClientId -clientName $myClientName -allowedScopes @("fabric/authorization.read", "dos/metadata", "fabric.profile") 
                $response | Should -not -Be :null
                $response.ClientId | Should -Be $myClientId

                #check default values
                $response.allowedGrantTypes | Should -Be "hybrid"
                $response.allowedScopes | Should -Contain "dos/metadata"
                $response.allowOfflineAccess | Should -Be $true
            }
        }
    }
}

describe 'New-HybridPkceClientBody' {
    Context 'Unit Tests' {
        Context 'Valid Object' {
            It 'Should return a valid object with required parameters' {          
                $myClientId = "validHybridPkceClient"
                $myClientName = "Valid Hybrid PKCE client name"

                $response = New-HybridPkceClientBody -clientId $myClientId -clientName $myClientName -allowedScopes @("fabric/authorization.read", "dos/metadata", "fabric.profile") 
                $response | Should -not -Be :null
                $response.ClientId | Should -Be $myClientId

                #check default values
                $response.allowedGrantTypes | Should -Be "hybrid"
                $response.allowedScopes | Should -Contain "dos/metadata"
                $response.updateAccessTokenClaimsOnRefresh | Should -Be $true
            }
        }
    }
}
describe 'New-HybridPkceClientBody' {
    Context 'Unit Tests' {
        Context 'Valid Object' {
            It 'Should return a valid object with required parameters' {          
                $myClientId = "validHybridPkceClient"
                $myClientName = "Valid Hybrid PKCE client name"

                $response = New-HybridPkceClientBody -clientId $myClientId -clientName $myClientName -allowedScopes @("fabric/authorization.read", "dos/metadata", "fabric.profile") 
                $response | Should -not -Be :null
                $response.ClientId | Should -Be $myClientId

                #check default values
                $response.allowedGrantTypes | Should -Be "hybrid"
                $response.allowedScopes | Should -Contain "dos/metadata"
                $response.updateAccessTokenClaimsOnRefresh | Should -Be $true
            }
        }
    }
}

describe 'Invoke-UpdateClientRegistration' {
    Context 'Unit Tests'{
        Context 'Valid Request' {
            It 'Should return nothing on successful update' {
                $authUrl = [System.Uri]'https://some.Server/identity'

                $newClient = New-ClientCredentialsClientBody `
                        -clientId "cliTestClient" `
                        -clientName "NEW cli Test Client Name" `
                        -allowedScopes @("fabric/identity.manageresources", "fabric/authorization.read", "fabric/authorization.write", "fabric/authorization.manageclients")

                $jsonClient = $newClient | ConvertTo-Json

                Mock -ModuleName identity-cli -CommandName Invoke-RestMethod { return ''} `
                -ParameterFilter {
                    $uri -match "$authUrl"
                } 

                $response = Invoke-UpdateClientRegistration -identityUrl $authUrl -body $jsonClient -accessToken "good token"

                $response | Should -Be ""
            }
        }

        Context 'Invalid request - bad json' {
            It 'Should return an error when invalid client json' {
                $authUrl = [System.Uri]'https://some.Server/identity'
    
                $newClient = @{}                                                                                                                                                    
                $newClient.Add("clientId", "cliTestClientssss")                                                                                                                         
                $newClient.Add("clientName", "cli Test Client Name")                                                                                                                
                $newClient.Add("requireConsent", "false")                                                                                                                           
    
                $jsonClient = $newClient | ConvertTo-Json
    
                Mock -ModuleName identity-cli -CommandName Invoke-RestMethod {
                    throw  New-Object -TypeName "System.Net.WebException" -ArgumentList "There was an error registering client" 
                } `
                -ParameterFilter {
                    $uri -match "$authUrl"
                } 
                
                {Invoke-UpdateClientRegistration -identityUrl $authUrl -body $jsonClient -accessToken "goodToken"} | Should -Throw "There was an error updating client registration" -ExceptionType System.Net.WebException
            }
        }
        Context 'Invalid request - bad token' {
    
            It 'Should return an error when bad token' {
                $authUrl = [System.Uri]'https://some.Server/identity'
                #$authUrl = [System.Uri]'https://hc2313.hqcatalyst.local/identity'

                $newClient = @{}                                                                                                                                                    
                $newClient.Add("clientId", "cliTestClientssss")                                                                                                                         
                $newClient.Add("clientName", "new cli Test Client Name")                                                                                                                
                $newClient.Add("requireConsent", "false")                                                                                                                           
    
                $jsonClient = $newClient | ConvertTo-Json

                Mock -ModuleName identity-cli -CommandName Invoke-RestMethod {
                    throw  New-Object -TypeName "System.Net.WebException" -ArgumentList "Unauthorized" 
                } `
                -ParameterFilter {
                    $uri -match "$authUrl"
                } 
                            
                # $x = Invoke-UpdateClientRegistration -identityUrl $authUrl  -body $jsonClient -accessToken  "Bearer badtoken"
                
                {Invoke-UpdateClientRegistration -identityUrl $authUrl  -body $jsonClient -accessToken  "Bearer badtoken"} | Should -Throw "Unauthorized" -ExceptionType System.Net.WebException
            }
        }
    }
}

describe 'Invoke-UpdateClientPassword' {
    Context 'Unit Tests'{
        Context 'Valid Request' {
            It 'Should return client string on successful reset' {
                # $authUrl = [System.Uri]'https://some.Server/identity'
                $authUrl = [System.Uri]'https://hc2313.hqcatalyst.local/identity'

                $myToken = 'eyJhbGciOiJSUzI1NiIsImtpZCI6IjhFNTA5RjY4MzFCNEJFNUQ4RUQ1QUM4NTZGRjI5NDYwRkE5NDRENEEiLCJ0eXAiOiJKV1QiLCJ4NXQiOiJqbENmYURHMHZsMk8xYXlGYl9LVVlQcVVUVW8ifQ.eyJuYmYiOjE1MzEzOTU2NDEsImV4cCI6MTUzMTM5OTI0MSwiaXNzIjoiaHR0cHM6Ly9oYzIzMTMuaHFjYXRhbHlzdC5sb2NhbC9pZGVudGl0eSIsImF1ZCI6WyJodHRwczovL2hjMjMxMy5ocWNhdGFseXN0LmxvY2FsL2lkZW50aXR5L3Jlc291cmNlcyIsInJlZ2lzdHJhdGlvbi1hcGkiXSwiY2xpZW50X2lkIjoiZmFicmljLWluc3RhbGxlciIsInNjb3BlIjpbImZhYnJpYy9pZGVudGl0eS5tYW5hZ2VyZXNvdXJjZXMiXX0.AP6rJLjMUNasGp0cqNyxP-9D3gGuToKJu4m_B4rjp95DbJbRiYCNMoKZyXMS-3wxLcW5FjEX0gHqqeDdzIRf6-W7phXW_FagJ4fHynxFT3ny-INcgm9Xri7qBnw3-HbtLpqxC1Sybp6yLmua0-893WdWws4nEjjTAGFcdgXqCi_0EsCRDTUWu__bPuiyRwWE9u1MnKbOL7w-egHxRYSh9UDxQrnueoOI-26A9HOAOtCHqHUsBNY2sR3IQaqjosph6UjXYFe6yZzb9b5augNz87gpQD7A6eovkLKo8h9tRXAIoM_4p16yHelIqBDqtuGAxADUZ_KC5i_j_6eqPRGUHA'
                
                $response = Invoke-UpdateClientPassword -identityUrl $authUrl -clientId "cliTestClient"  -accessToken $myToken
                $response | Should -Not -Be :null
                $response | Should -Not -Be ""
                $response.Length | Should -BeGreaterThan 8
            }
        }
    }
}

describe 'Test-IsClientRegistered' {
    Context 'Unit Tests'{
        Context 'Valid Request' {
            It 'Should return client string on successful reset' {
                # $authUrl = [System.Uri]'https://some.Server/identity'
                $authUrl = [System.Uri]'https://hc2313.hqcatalyst.local/identity'

                $myToken = 'sss eyJhbGciOiJSUzI1NiIsImtpZCI6IjhFNTA5RjY4MzFCNEJFNUQ4RUQ1QUM4NTZGRjI5NDYwRkE5NDRENEEiLCJ0eXAiOiJKV1QiLCJ4NXQiOiJqbENmYURHMHZsMk8xYXlGYl9LVVlQcVVUVW8ifQ.eyJuYmYiOjE1MzEzOTU2NDEsImV4cCI6MTUzMTM5OTI0MSwiaXNzIjoiaHR0cHM6Ly9oYzIzMTMuaHFjYXRhbHlzdC5sb2NhbC9pZGVudGl0eSIsImF1ZCI6WyJodHRwczovL2hjMjMxMy5ocWNhdGFseXN0LmxvY2FsL2lkZW50aXR5L3Jlc291cmNlcyIsInJlZ2lzdHJhdGlvbi1hcGkiXSwiY2xpZW50X2lkIjoiZmFicmljLWluc3RhbGxlciIsInNjb3BlIjpbImZhYnJpYy9pZGVudGl0eS5tYW5hZ2VyZXNvdXJjZXMiXX0.AP6rJLjMUNasGp0cqNyxP-9D3gGuToKJu4m_B4rjp95DbJbRiYCNMoKZyXMS-3wxLcW5FjEX0gHqqeDdzIRf6-W7phXW_FagJ4fHynxFT3ny-INcgm9Xri7qBnw3-HbtLpqxC1Sybp6yLmua0-893WdWws4nEjjTAGFcdgXqCi_0EsCRDTUWu__bPuiyRwWE9u1MnKbOL7w-egHxRYSh9UDxQrnueoOI-26A9HOAOtCHqHUsBNY2sR3IQaqjosph6UjXYFe6yZzb9b5augNz87gpQD7A6eovkLKo8h9tRXAIoM_4p16yHelIqBDqtuGAxADUZ_KC5i_j_6eqPRGUHA'
                
                Mock -ModuleName identity-cli -CommandName Invoke-RestMethod {
                    $errorDetails =  '{"code": 21212, "message": "error message form ryan", "status": 411}'
                    $statusCode = 400
                    $response = [System.Net.Http.HttpResponseMessage] $statusCode
                    $exception = New-Object System.Net.Http.HttpResponseException "$statusCode ($($response.ReasonPhrase))", $response
                
                    $errorCategory = [System.Management.Automation.ErrorCategory]::InvalidOperation
                    
                    $errorID = 'WebCmdletWebResponseException,Microsoft.PowerShell.Commands.InvokeWebRequestCommand'
                    $targetObject = $null
                    
                    $errorRecord = New-Object Management.Automation.ErrorRecord $exception, $errorID, $errorCategory, $targetObject
                    $errorRecord.ErrorDetails = $errorDetails
        
                    Throw $errorRecord                    
                } `
                -ParameterFilter {
                    $uri -match "$authUrl"
                } 

                $response = Test-IsClientRegistered -identityUrl $authUrl -clientId "cliTestClient ddd"  -accessToken $myToken
                $response | Should -Not -Be :null
                $response | Should -Be $true
            }
        }
    }
}

describe 'Get-ApiRegistration' {}

describe 'New-ApiRegistration' {}

describe 'Invoke-UpdateApiRegistration' {}

describe 'Invoke-UpdateClientPassword' {}

describe 'Test-IsApiRegistered' {}
