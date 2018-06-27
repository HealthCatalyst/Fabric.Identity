param(
    [Uri] $identityUrl = "http://localhost:5001"
)

describe 'Functional-Test' {    
    BeforeAll {
        # Set up auth/identity
        Start-Process ..\start-auth-identity.sh -Wait
    }
    AfterAll {
        # Tear down auth/identity
        Start-Process ..\stop-auth-identity.sh -Wait
    }
    
    It 'Should return an access token when valid response' {
        [Uri] $url = [Uri]"$identityUrl.well-known/openid-configuration"

        $response = Invoke-RestMethod -Method Get -Uri $url
        $response.issuer | Should -Be "http://functional-identity:5001"
    }
}