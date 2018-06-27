param(
    [string] $targetFilePath = "$PSScriptRoot\..\identity-cli.psm1",
    [Uri] $identityUrl = "http://localhost:5001"
)

# Force re-import to pick up latest changes
Import-Module $targetFilePath -Force

describe 'Functional-Test' {    
    BeforeAll {
        # Set up auth/identity
        Start-Process "$targetFilePath\..\start-auth-identity.sh" -Wait
    }
    AfterAll {
        # Tear down auth/identity
        Start-Process "$targetFilePath\..\stop-auth-identity.sh" -Wait
    }
    
    It 'Should return an access token when valid response' {
        [Uri] $url = [Uri]"$identityUrl.well-known/openid-configuration"

        $response = Invoke-RestMethod -Method Get -Uri $url
        $response.issuer | Should -Be "http://functional-identity:5001"
    }
}