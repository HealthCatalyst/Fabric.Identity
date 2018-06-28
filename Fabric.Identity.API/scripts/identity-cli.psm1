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
        [string[]] $scope
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
    Attempts to get an access token for the fabric installer client

    .Description
    Takes in the identity server url and the installer secret and return the installer access token.

    .Parameter identityUrl
    The identity url to get the access token from

    .Parameter secret
    The secret of the fabric installer client

    .Example
    Get-AccessToken -identityUrl "https://server/identity" -secret "SECrEtStrING" 
#>
function Get-FabricInstallerAccessToken {
    param(
        [Parameter(Mandatory=$True)] [Uri] $identityUrl,
        [Parameter(Mandatory=$True)] [string] $secret
    )

    $clientId = "fabric-installer"
    $scope = "fabric/identity.manageresources", "fabric/authorization.read", "fabric/authorization.write", "fabric/authorization.dos.write", "fabric/authorization.manageclients"
    return Get-AccessToken $identityUrl $clientId $secret $scope
}

<#
    .Synopsis
    Attempts to retrive information about an existing identity client

    .Description
    Takes in the identity server url and the clientid and an access token.

    .Parameter identityUrl
    The identity url to get the access token from

    .Parameter clientId
    the identifier for the client to retrieve

    .Parameter accessToken
    an access token previously retrieved from the identity server

    .Example
    Get-ClientRegistration -identityUrl "https://server/identity" -clientId "sample-client-id" -accessToken "eyJhbGciO"
#>
function Get-ClientRegistration {
    param(
        [Parameter(Mandatory=$True)] [Uri] $identityUrl,
        [Parameter(Mandatory=$True)] [string] $clientId,
        [Parameter(Mandatory=$True)] [string] $accessToken
    )

    $url = "$identityUrl/api/v1/client/$clientId"

    $headers = @{"Accept" = "application/json"}
    $headers.Add("Authorization", "Bearer $accessToken")

    $clientResponse = Invoke-RestMethod -Method Get -Uri $url -Headers $headers
    return $clientResponse
}

<#
    .Synopsis
    Attempts to register a new identity client

    .Description
    Takes in the identity server url and the client body and an access token.
    Returns a client secret for the new client.
    If the client already exists, this function updates the client attributes and resets the client secret.

    .Parameter identityUrl
    The identity url to get the access token from

    .Parameter body
    the json payload of attributes and values for the new client

    .Parameter accessToken
    an access token previously retrieved from the identity server

    .Example
    Get-ClientRegistration -identityUrl "https://server/identity" -body @'{"clientId":"fabric-installer", "clientName":"Fabric Installer" ...... }@' -accessToken "eyJhbGciO"
#>
function New-ClientRegistration {
    param(
        [Parameter(Mandatory=$True)] [Uri] $identityUrl,
        [Parameter(Mandatory=$True)] [string] $body,
        [Parameter(Mandatory=$True)] [string] $accessToken
    )

    $url = "$identityUrl/api/client"
    $headers = @{"Accept" = "application/json"}
    if($accessToken){
        $headers.Add("Authorization", "Bearer $accessToken")
    }
    
    # attempt to add
    try{
        $registrationResponse = Invoke-RestMethod -Method Post -Uri $url -Body $body -ContentType "application/json" -Headers $headers
        return $registrationResponse.clientSecret
    }catch{
        $exception = $_.Exception
        $clientObject = ConvertFrom-Json -InputObject $body
        if ($exception -ne $null -and $exception.Response.StatusCode.value__ -eq 409) {
            Write-Warning "Client $($clientObject.clientName) is already registered...updating registration settings."
            try{                
                Invoke-RestMethod -Method Put -Uri "$url/$($clientObject.clientId)" -Body $body -ContentType "application/json" -Headers $headers

                # Reset client secret
                $apiResponse = Invoke-RestMethod -Method Post -Uri "$url/$($clientObject.clientId)/resetPassword" -ContentType "application/json" -Headers $headers
                return $apiResponse.clientSecret
            }catch{
                $exception = $_.Exception
                $error = Get-ErrorFromResponse -response $exception.Response
                Write-Error "There was an error updating Client $($clientObject.clientName): $error. Halting installation."
                throw $exception
            }
        }
        else {
            $error = "Unknown error."
            $exception = $_.Exception
            if($exception -ne $null -and $exception.Response -ne $null){
                $error = Get-ErrorFromResponse -response $exception.Response
            }
            Write-Error "There was an error registering client $($clientObject.clientName) with Fabric.Identity: $error, halting installation."
            throw $exception
        }
    }    
}

function New-ImplicitClientRegistration($identityUrl, $body, $accessToken) {
    throw [System.NotImplementedException]
}

function New-HybridClientRegistration($identityUrl, $body, $accessToken) {
    throw [System.NotImplementedException]
}

function New-HybridPkceClientRegistration($identityUrl, $body, $accessToken) {
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

function New-ApiRegistration($identityUrl, $body, $accessToken) {
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

function Get-ErrorFromResponse($response) {
    $result = $response.GetResponseStream()
    $reader = New-Object System.IO.StreamReader($result)
    $reader.BaseStream.Position = 0
    $reader.DiscardBufferedData()
    $responseBody = $reader.ReadToEnd();
    return $responseBody
}

Export-ModuleMember -Function Get-AccessToken
Export-ModuleMember -Function Get-FabricInstallerAccessToken
Export-ModuleMember -Function Get-ClientRegistration
Export-ModuleMember -Function New-ClientRegistration
Export-ModuleMember -Function New-ImplicitClientRegistration
Export-ModuleMember -Function New-HybridClientRegistration
Export-ModuleMember -Function New-HybridPkceClientRegistration
Export-ModuleMember -Function Invoke-UpdateClientRegistration
Export-ModuleMember -Function Invoke-UpdateClientPassword
Export-ModuleMember -Function Test-IsClientRegistered
Export-ModuleMember -Function Get-ApiRegistration
Export-ModuleMember -Function New-ApiRegistration
Export-ModuleMember -Function Invoke-UpdateApiRegistration
Export-ModuleMember -Function Invoke-UpdateClientPassword
Export-ModuleMember -Function Test-IsApiRegistered
Export-ModuleMember -Function Get-ErrorFromResponse