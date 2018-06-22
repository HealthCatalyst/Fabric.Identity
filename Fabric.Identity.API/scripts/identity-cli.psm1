<#
    .Synopsis
    Attempts to get an access token for given client

    .Description
    Takes in a client id and secret with valid scope and hits the identity server provided and returns an access token.

    .Parameter identityUrl
    The identity url to get the access token from

    .Parameter clientId
    The id of the client the access token is being requested for

    .Parameter secret
    The secret of the client the access token is being requested for

    .Parameter scopes
    The scope of the access token to be requested

    .Example
    Get-AccessToken -identityUrl "https://server/identity" -clientId "sample-client-id" -secret "SECrEtStrING" -scopes "offline_access"
#>
function Get-AccessToken {
    param(
        [Parameter(Mandatory=$True)] [Uri] $identityUrl,
        [Parameter(Mandatory=$True)] [string] $clientId,
        [Parameter(Mandatory=$True)] [string] $secret,
        [Parameter(Mandatory=$True)] [string] $scope
    )

    $url = "$identityUrl/connect/token"
    $body = @{
        client_id = "$clientId"
        grant_type = "client_credentials"
        scope = "$scope"
        client_secret = "$secret"
    }

    $accessTokenResponse = Invoke-RestMethod -Method Post -Uri $url -Body $body
    return $accessTokenResponse.access_token
}

<#
    .Synopsis
    Attempts to get an access token the fabric installer client

    .Description
    Takes in the identity server url and the installer secret and return the installer access token.

    .Parameter identityUrl
    The identity url to get the access token from

    .Parameter secret
    The secret of the fabric installer client

    .Parameter scopes
    The scope of the access token to be requested

    .Example
    Get-AccessToken -identityUrl "https://server/identity"-secret "SECrEtStrING" 
#>
function Get-FabricInstallerAccessToken {
    param(
        [Parameter(Mandatory=$True)] [Uri] $identityUrl,
        [Parameter(Mandatory=$True)] [string] $secret
    )

    $clientId = "fabric-installer"
    $scope = "fabric/identity.manageresources"
    $return =  Get-AccessToken $identityUrl $clientId $secret $scope
    return $return
}

function Get-ClientRegistration($identityUrl, $clientId, $accessToken) {
    throw [System.NotImplementedException]
}

function Create-ClientRegistration($identityUrl, $body, $accessToken) {
    throw [System.NotImplementedException]
}

function Create-ImplicitClientRegistration($identityUrl, $body, $accessToken) {
    throw [System.NotImplementedException]
}

function Create-HybridClientRegistration($identityUrl, $body, $accessToken) {
    throw [System.NotImplementedException]
}

function Create-HybridPkceClientRegistration($identityUrl, $body, $accessToken) {
    throw [System.NotImplementedException]
}

function Invoke-UpdateClientRegistration($identityUrl, $body, $accessToken) {
    throw [System.NotImplementedException]
}

function Invoke-UpdateClientPassword($identityUrl, $accessToken) {
    throw [System.NotImplementedException]
}

function Test-IsClientRegistered($identityUrl, $clientId, $accessToken) {
    throw [System.NotImplementedException]
}

function Get-ApiRegistration($identityUrl, $apiName, $accessToken) {
    throw [System.NotImplementedException]
}

function Create-ApiRegistration($identityUrl, $body, $accessToken) {
    throw [System.NotImplementedException]
}

function Invoke-UpdateApiRegistration($identityUrl, $body, $accessToken) {
    throw [System.NotImplementedException]
}

function Invoke-UpdateClientPassword($identityUrl, $accessToken) {
    throw [System.NotImplementedException]
}

function Test-IsApiRegistered($identityUrl, $apiName, $accessToken) {
    throw [System.NotImplementedException]
}


Export-ModuleMember -Function Get-AccessToken
Export-ModuleMember -Function Get-FabricInstallerAccessToken
Export-ModuleMember -Function Get-ClientRegistration
Export-ModuleMember -Function Create-ClientRegistration
Export-ModuleMember -Function Create-ImplicitClientRegistration
Export-ModuleMember -Function Create-HybridClientRegistration
Export-ModuleMember -Function Create-HybridPkceClientRegistration
Export-ModuleMember -Function Invoke-UpdateClientRegistration
Export-ModuleMember -Function Invoke-UpdateClientPassword
Export-ModuleMember -Function Test-IsClientRegistered
Export-ModuleMember -Function Get-ApiRegistration
Export-ModuleMember -Function Create-ApiRegistration
Export-ModuleMember -Function Invoke-UpdateApiRegistration
Export-ModuleMember -Function Invoke-UpdateClientPassword
Export-ModuleMember -Function Test-IsApiRegistered