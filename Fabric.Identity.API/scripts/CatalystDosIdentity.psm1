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
        [Parameter(Mandatory = $True)] [Uri] $identityUrl,
        [Parameter(Mandatory = $True)] [string] $clientId,
        [Parameter(Mandatory = $True)] [string] $secret,
        [string] $scope
    )

    [Uri] $url = "$($identityUrl.OriginalString)/connect/token"
    $body = @{
        client_id     = $clientId
        grant_type    = "client_credentials"
        scope         = $scope
        client_secret = $secret
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
        [Parameter(Mandatory = $True)] [Uri] $identityUrl,
        [Parameter(Mandatory = $True)] [string] $secret
    )

    $clientId = "fabric-installer"
    $scope = "fabric/identity.manageresources fabric/authorization.read fabric/authorization.write fabric/authorization.dos.write fabric/authorization.manageclients"
    return Get-AccessToken $identityUrl $clientId $secret $scope
}

<#
    .Synopsis
    Attempts to retrive information about an existing identity client

    .Description
    Takes in the identity server url, the clientid and an access token.
    Returns a client identity object

    .Parameter identityUrl
    The base identity url

    .Parameter clientId
    the identifier for the client to retrieve

    .Parameter accessToken
    an access token previously retrieved from the identity server

    .Example
    Get-ClientRegistration -identityUrl "https://server/identity" -clientId "sample-client-id" -accessToken "eyJhbGciO"
#>
function Get-ClientRegistration {
    param(
        [Parameter(Mandatory = $True)] [Uri] $identityUrl,
        [Parameter(Mandatory = $True)] [string] $clientId,
        [Parameter(Mandatory = $True)] [string] $accessToken
    )

    [Uri]$url = "$($identityUrl.OriginalString)/api/v1/client/$clientId"

    $headers = @{"Accept" = "application/json"}
    $headers.Add("Authorization", "Bearer $accessToken")

    $clientResponse = Invoke-RestMethod -Method Get -Uri $url -Headers $headers
    return $clientResponse
}

<#
    .Synopsis
    Attempts to register a new identity client

    .Description
    Takes in the identity server url, the json client body and an access token.
    Returns a client secret for the new client.
    If the client already exists, this function updates the client attributes and resets the client secret.

    .Parameter identityUrl
    The base identity url

    .Parameter body
    the json payload of attributes and values for the new client

    .Parameter accessToken
    an access token previously retrieved from the identity server

    .Example
    New-ClientRegistration -identityUrl "https://server/identity" -body '{"clientId":"fabric-installer", "clientName":"Fabric Installer" ...... }' -accessToken "eyJhbGciO"
#>
function New-ClientRegistration {
    param(
        [Parameter(Mandatory = $True)] [Uri] $identityUrl,
        [Parameter(Mandatory = $True)] [string] $body,
        [Parameter(Mandatory = $True)] [string] $accessToken
    )

    $url = "$identityUrl/api/client"
    $headers = @{"Accept" = "application/json"}
    if ($accessToken) {
        $headers.Add("Authorization", "Bearer $accessToken")
    }

    # attempt to add
    try {
        $registrationResponse = Invoke-RestMethod -Method Post -Uri $url -Body $body -ContentType "application/json" -Headers $headers
        return $registrationResponse.clientSecret
    }
    catch {
        $exception = $_.Exception
        $clientObject = ConvertFrom-Json -InputObject $body
        if ((Assert-WebExceptionType -exception $exception -typeCode 409)) {
            try {
                # client ID already exists, update with PUT
                Invoke-RestMethod -Method Put -Uri "$url/$($clientObject.clientId)" -Body $body -ContentType "application/json" -Headers $headers | out-null

                # Reset client secret
                $apiResponse = Invoke-RestMethod -Method Post -Uri "$url/$($clientObject.clientId)/resetPassword" -ContentType "application/json" -Headers $headers
                return $apiResponse.clientSecret
            }
            catch {
                $error = "Unknown error attempting to update"
                $exception = $_.Exception
                if ($null -ne $exception -and $null -ne $exception.Response) {
                    $error = Get-ErrorFromResponse -response $exception.Response
                }
                throw (New-Object -TypeName "System.Net.WebException" "There was an error updating Client $($clientObject.clientName): $error. Halting installation.", $exception)
            }
        }
        else {
            $error = "Unknown error attempting to post"
            $exception = $_.Exception
            if ($null -ne $exception -and $null -ne $exception.Response) {
                $error = Get-ErrorFromResponse -response $exception.Response
            }
            throw ( New-Object -TypeName "System.Net.WebException" "There was an error registering client $($clientObject.clientName) with Fabric.Identity: $error, halting installation.", $exception)
        }
    }
}

<#
    .Synopsis
    Creates a Client Credentials Identity Client

    .Description
    Retruns a new hashtable that represents a valid identity client body for us with New-ClientRegistration. Sensible defaults values are included.

    .Parameter clientId
    The text identiifier for the client

    .Parameter clientName
    The human readable text name for the client

    .Parameter allowedScopes
    Array of strings representing the allowed scopes for the client

    .Example
    New-ClientCredentialsClientBody -clientId "sample-implicit-client" -clientName "Sample Client using Implicit Registration" -allowesScopes @("dos/metadata.read")
#>
function New-ClientCredentialsClientBody {
    param(
        [Parameter(Mandatory = $True)] [string] $clientId,
        [Parameter(Mandatory = $True)] [string] $clientName,
        [Parameter(Mandatory = $True)] [AllowEmptyCollection()] [string[]] $allowedScopes
    )

    # parameters
    $newClient = @{}
    $newClient.Add("clientId", $clientId)
    $newClient.Add("clientName", $clientName)
    $newClient.Add("allowedScopes", $allowedScopes)

    $newClient.Add("allowedGrantTypes", @("client_credentials"))

    return $newClient
}

<#
    .Synopsis
    Creates an Implicit Identity Client

    .Description
    Retruns a new hashtable that represents a valid identity client body for us with New-ClientRegistration. Sensible defaults values are included.

    .Parameter clientId
    The text identiifier for the client

    .Parameter clientName
    The human readable text name for the client

    .Parameter allowedScopes
    Array of strings representing the allowed scopes for the client

    .Parameter allowedCorsOrigins
    Array of strings representing the allowed addresses for Cross-Origin Resource Sharing

    .Parameter redirectUris
    Array of strings representing list of uri's that can request a login redirect

    .Parameter postLogoutRedirectUris
    Array of strings representing list of uri's that are accetable navigation after logout

    .Example
    New-ImplicitClientBody -clientId "sample-implicit-client" -clientName "Sample Client using Implicit Registration" -allowesScopes @("dos/metadata.read") -allowesCorsOrigins @('http://some.server', 'https://some.other.server') -redirectUris @('https://some.server/loginRedirect') -postLogoutRedirectUris @('https://some.other.server/logoutRedirect')
#>
function New-ImplicitClientBody {
    param(
        [Parameter(Mandatory = $True)] [string] $clientId,
        [Parameter(Mandatory = $True)] [string] $clientName,
        [Parameter(Mandatory = $True)] [AllowEmptyCollection()] [string[]] $allowedScopes,
        [Parameter(Mandatory = $false)] [string[]] $allowedCorsOrigins,
        [Parameter(Mandatory = $false)] [string[]] $redirectUris,
        [Parameter(Mandatory = $false)] [string[]] $postLogoutRedirectUris
    )

    # parameters
    $newClient = @{}
    $newClient.Add("clientId", $clientId)
    $newClient.Add("clientName", $clientName)
    $newClient.Add("allowedScopes", $allowedScopes)
    $newClient.Add("allowedCorsOrigins", $allowedCorsOrigins)
    $newClient.Add("redirectUris", $redirectUris )
    $newClient.Add("postLogoutRedirectUris", $postLogoutRedirectUris)

    $newClient.Add("allowedGrantTypes", @("implicit"))
    $newClient.Add("requireConsent", $false)
    $newClient.Add("allowOfflineAccess", $false)
    $newClient.Add("allowAccessTokensViaBrowser", $true)
    $newClient.Add("enableLocalLogin", $false)
    $newClient.Add("accessTokenLifetime", 1200)

    return $newClient
}

<#
    .Synopsis
    Creates an Hybryd Identity Client

    .Description
    Retruns a new hashtable that represents a valid identity client body for us with New-ClientRegistration. Sensible defaults values are included.

    .Parameter clientId
    The text identiifier for the client

    .Parameter clientName
    The human readable text name for the client

    .Parameter allowedScopes
    Array of strings representing the allowed scopes for the client

    .Parameter allowedCorsOrigins
    Array of strings representing the allowed addresses for Cross-Origin Resource Sharing

    .Parameter redirectUris
    Array of strings representing list of uri's that can request a login redirect

    .Parameter postLogoutRedirectUris
    Array of strings representing list of uri's that are accetable navigation after logout

    .Example
    New-HybridClientBody -clientId "sample-implicit-client" -clientName "Sample Client using Implicit Registration" -allowesScopes @("dos/metadata.read") -allowesCorsOrigins @('http://some.server', 'https://some.other.server') -redirectUris @('https://some.server/loginRedirect') -postLogoutRedirectUris @('https://some.other.server/logoutRedirect')
#>
function New-HybridClientBody {
    param(
        [Parameter(Mandatory = $True)] [string] $clientId,
        [Parameter(Mandatory = $True)] [string] $clientName,
        [Parameter(Mandatory = $True)] [AllowEmptyCollection()] [string[]] $allowedScopes,
        [Parameter(Mandatory = $false)] [string[]] $allowedCorsOrigins,
        [Parameter(Mandatory = $false)] [string[]] $redirectUris,
        [Parameter(Mandatory = $false)] [string[]] $postLogoutRedirectUris
    )

    # parameters
    $newClient = @{}
    $newClient.Add("clientId", $clientId)
    $newClient.Add("clientName", $clientName)
    $newClient.Add("allowedScopes", $allowedScopes)
    $newClient.Add("allowedCorsOrigins", $allowedCorsOrigins)
    $newClient.Add("redirectUris", $redirectUris )
    $newClient.Add("postLogoutRedirectUris", $postLogoutRedirectUris)

    $newClient.Add("allowedGrantTypes", @("hybrid"))
    $newClient.Add("requireConsent", $false)
    $newClient.Add("allowOfflineAccess", $true)
    $newClient.Add("allowAccessTokensViaBrowser", $false)
    $newClient.Add("enableLocalLogin", $false)

    return $newClient
}

<#
    .Synopsis
    Creates an Hybryd and PKCE Identity Client

    .Description
    Retruns a new hashtable that represents a valid identity client body for us with New-ClientRegistration. Sensible defaults values are included.

    .Parameter clientId
    The text identiifier for the client

    .Parameter clientName
    The human readable text name for the client

    .Parameter allowedScopes
    Array of strings representing the allowed scopes for the client

    .Parameter redirectUris
    Array of strings representing list of uri's that can request a login redirect

    .Example
    New-HybridPkceClientBody -clientId "sample-implicit-client" -clientName "Sample Client using Implicit Registration" -allowesScopes @("dos/metadata.read") -redirectUris @('https://some.server/loginRedirect')
#>
function New-HybridPkceClientBody {
    param(
        [Parameter(Mandatory = $True)] [string] $clientId,
        [Parameter(Mandatory = $True)] [string] $clientName,
        [Parameter(Mandatory = $True)] [AllowEmptyCollection()] [string[]] $allowedScopes,
        [Parameter(Mandatory = $false)] [string[]] $redirectUris
    )

    # parameters
    $newClient = @{}
    $newClient.Add("clientId", $clientId)
    $newClient.Add("clientName", $clientName)
    $newClient.Add("allowedScopes", $allowedScopes)
    $newClient.Add("redirectUris", $redirectUris )

    $newClient.Add("allowedGrantTypes", @("hybrid"))
    $newClient.Add("requireConsent", $false)
    $newClient.Add("requireClientSecret", $false)
    $newClient.Add("allowOfflineAccess", $true)
    $newClient.Add("requirePkce", $true)
    $newClient.Add("updateAccessTokenClaimsOnRefresh", $true)

    return $newClient
}

<#
    .Synopsis
    Attempts to update an identity client

    .Description
    Takes in the identity server url, the json client body and an access token.

    .Parameter identityUrl
    The identity url to get the access token from

    .Parameter body
    the json payload of attributes and values for the new client

    .Parameter accessToken
    an access token previously retrieved from the identity server

    .Example
    Edit-ClientRegistration -identityUrl "https://server/identity" -body @'{"clientId":"fabric-installer", "clientName":"Fabric Installer" ...... }@' -accessToken "eyJhbGciO"
#>
function Edit-ClientRegistration {
    param(
        [Parameter(Mandatory = $True)] [Uri] $identityUrl,
        [Parameter(Mandatory = $True)] [string] $body,
        [Parameter(Mandatory = $True)] [string] $accessToken
    )

    $clientObject = ConvertFrom-Json -InputObject $body
    $url = "$identityUrl/api/client"
    $headers = @{"Accept" = "application/json"}
    if ($accessToken) {
        $headers.Add("Authorization", "Bearer $accessToken")
    }

    # attempt to PUT
    try {
        $updateReturn = Invoke-RestMethod -Method Put -Uri "$url/$($clientObject.clientId)" -Body $body -ContentType "application/json" -Headers $headers
        return $updateReturn
    }
    catch {
        $error = "Unknown error."
        $exception = $_.Exception
        if ($null -ne $exception -and $null -ne $exception.Response) {
            $error = Get-ErrorFromResponse -response $exception.Response
        }
        throw ( New-Object -TypeName "System.Net.WebException" "There was an error updating client registration $($clientObject.clientName) with Fabric.Identity: $error, halting installation.", $exception)
    }
}

<#
    .Synopsis
    Attempts to update the client secret of an identity client

    .Description
    Takes in the identity server url, the json client ID and an access token.

    .Parameter identityUrl
    The identity url to get the access token from

    .Parameter clientId
    the text identiified for the client to reset

    .Parameter accessToken
    an access token previously retrieved from the identity server

    .Example
    Reset-ClientPassword -identityUrl "https://server/identity" -clientId "someClient" -accessToken "eyJhbGciO"
#>
function Reset-ClientPassword {
    param(
        [Parameter(Mandatory = $True)] [Uri] $identityUrl,
        [Parameter(Mandatory = $True)] [string] $clientId,
        [Parameter(Mandatory = $True)] [string] $accessToken
    )

    $url = "$identityUrl/api/client"
    $headers = @{"Accept" = "application/json"}
    if ($accessToken) {
        $headers.Add("Authorization", "Bearer $accessToken")
    }

    # attempt to Reset
    try {
        $apiResponse = Invoke-RestMethod -Method Post -Uri "$url/$clientId/resetPassword" -ContentType "application/json" -Headers $headers
        return $apiResponse.clientSecret
    }
    catch {
        $error = "Unknown error."
        $exception = $_.Exception
        if ($null -ne $exception -and $null -ne $exception.Response) {
            $error = Get-ErrorFromResponse -response $exception.Response
        }
        throw ( New-Object -TypeName "System.Net.WebException" "There was an error resetting client secret $clientId with Fabric.Identity: $error, halting installation.", $exception)
    }
}

<#
    .Synopsis
    checks for the existence of a specific identity client

    .Description
    Takes in the identity server url, the json client ID and an access token.

    .Parameter identityUrl
    The identity url to get the access token from

    .Parameter clientId
    the text identiified for the client to check

    .Parameter accessToken
    an access token previously retrieved from the identity server

    .Example
    Test-IsClientRegistered -identityUrl "https://server/identity" -clientId "someClient" -accessToken "eyJhbGciO"
#>
function Test-IsClientRegistered {
    param(
        [Parameter(Mandatory = $True)] [Uri] $identityUrl,
        [Parameter(Mandatory = $True)] [string] $clientId,
        [Parameter(Mandatory = $True)] [string] $accessToken
    )

    $url = "$identityUrl/api/v1/client/$clientId"

    $headers = @{"Accept" = "application/json"}
    $headers.Add("Authorization", "Bearer $accessToken")

    try {
        Invoke-RestMethod -Method Get -Uri $url -Headers $headers | Out-Null
        # exception thrown if not found
        return $True
    }
    catch {
        $exception = $_.Exception
        if (Assert-WebExceptionType -exception $exception -typeCode 404) {
            try {
                return $false
            }
            catch {
                $error = "Unknown error."
                $exception = $_.Exception
                if ($null -ne $exception -and $null -ne $exception.Response) {
                    $error = Get-ErrorFromResponse -response $exception.Response
                }
                throw ( New-Object -TypeName "System.Net.WebException" "There was an error looking for client $clientId with Fabric.Identity: $error, halting installation.", $exception)
            }
        }
        else {
            $error = "Unknown error."
            $exception = $_.Exception
            if ($null -ne $exception -and $null -ne $exception.Response) {
                $error = Get-ErrorFromResponse -response $exception.Response
            }
            throw ( New-Object -TypeName "System.Net.WebException" "There was an error looking for client $clientId with Fabric.Identity: $error, halting installation.", $exception)
        }
    }
}

<#
    .Synopsis
    Attempts to retrieve information about an existing identity api

    .Description
    Takes in the identity server url the apiname and an access token.

    .Parameter identityUrl
    The identity url to get the api registration

    .Parameter apiName
    The name of the api

    .Parameter accessToken
    An access token previously retrieved from the identity server

    .Example
    Get-ApiRegistration -identityUrl "https://server/identity" -apiName "TestAPI" -accessToken "eyJhbGciO"
#>
function Get-ApiRegistration {
	param(
        [Parameter(Mandatory=$True)] [Uri] $identityUrl,
        [Parameter(Mandatory=$True)] [string] $apiName,
        [Parameter(Mandatory=$True)] [string] $accessToken
    )

	[Uri]$url = "$($identityUrl.OriginalString)/api/apiresource/$apiName"

    $headers = @{"Accept" = "application/json"}
    if($accessToken){
        $headers.Add("Authorization", "Bearer $accessToken")
    }

    $clientResponse = Invoke-RestMethod -Method Get -Uri $url -Headers $headers
    return $clientResponse
}

<#
    .Synopsis
    Attempts to register an api with fabric identity

    .Description
    Takes in the identity server url the json request body and an access token.

    .Parameter identityUrl
    The identity url to post the api registration

    .Parameter body
    The json request format for creating the api registration

    .Parameter accessToken
    An access token previously retrieved from the identity server

    .Example
    New-ApiRegistration -identityUrl "https://server/identity" -body "{"enabled":true, "name": "sample-api", "userClaims":[], "scopes":[]}" -accessToken "eyJhbGciO"
#>
function New-ApiRegistration {
	param(
        [Parameter(Mandatory=$True)] [Uri] $identityUrl,
        [Parameter(Mandatory=$True)] [string] $body,
        [Parameter(Mandatory=$True)] [string] $accessToken
    )

    $url = "$identityUrl/api/apiresource"
    $headers = @{"Accept" = "application/json"}
    if($accessToken){
        $headers.Add("Authorization", "Bearer $accessToken")
    }

    try{
        $registrationResponse = Invoke-RestMethod -Method Post -Uri $url -body $body -ContentType "application/json" -Headers $headers
        return $registrationResponse.apiSecret
    }catch{
        $exception = $_.Exception
        $apiResourceObject = ConvertFrom-Json -InputObject $body
        if ((Assert-WebExceptionType -exception $exception -typeCode 409)) {
            try{
                Invoke-RestMethod -Method Put -Uri "$url/$($apiResourceObject.name)" -Body $body -ContentType "application/json" -Headers $headers | out-null

                # Reset api secret
                $apiResponse = Reset-ApiPassword -identityUrl $url -apiName $($apiResourceObject.name) -accessToken $accessToken
                return $apiResponse.apiSecret
            }catch{
                $error = "Unknown error attempting to post api"
                $exception = $_.Exception
                if ($null -ne $exception -and $null -ne $exception.Response) {
                    $error = Get-ErrorFromResponse -response $exception.Response
                }
                throw (New-Object -TypeName "System.Net.WebException" "There was an error registering api $($apiResourceObject.name): $error. Registration failure.", $exception)
            }
        }
        else {
            $error = "Unknown error attempting to post"
            $exception = $_.Exception
            if ($null -ne $exception -and $null -ne $exception.Response) {
                $error = Get-ErrorFromResponse -response $exception.Response
            }
            throw ( New-Object -TypeName "System.Net.WebException" "There was an error registering api $($apiResourceObject.name) with Fabric.Identity: $error, Registration failure.", $exception)
        }
    }
}

<#
    .Synopsis
    Attempts to create an apiresource object

    .Description
    Takes in the apiname userclaims scopes and isenabled.

    .Parameter apiName
    The name of the api
    
    .Parameter userClaims
    Array of strings representing the user claims

    .Parameter scopes
    Array of Hashtables representing the scopes for the api
    
    .Parameter isEnabled
    If the apiresource is enabled

    .Example
    New-ApiRegistrationBody -apiName "this-Api" -userClaims @("name", "email", "role", "groups") -scopes @{"name" = "this-Api"; "displayName" = "This-API"} -isEnabled true
#>
function New-ApiRegistrationBody {
	param(
        [Parameter(Mandatory=$True)] [string] $apiName,
        [string[]] $userClaims,
        [Hashtable[]] $scopes,
        [string] $isEnabled
    )

    $newApiResource = @{}
    $newApiResource.Add("name", $apiName)
    $newApiResource.Add("scopes", $scopes)
    $newApiResource.Add("userClaims", $userClaims)
    $newApiResource.Add("enabled", $isEnabled)

    return $newApiResource
}

<#
    .Synopsis
    Attempts to remove the api object

    .Description
    Takes in the identity server url an apiname and an access token.

    .Parameter identityUrl
    The identity url to delete the api registration

    .Parameter apiName
    The name of the api

    .Parameter accessToken
    An access token previously retrieved from the identity server

    .Example
    Remove-ApiRegistration -identityUrl "https://server/identity" -apiName "TestAPI" -accessToken "eyJhbGciO"
#>
function Remove-ApiRegistration {
	param(
        [Parameter(Mandatory=$True)] [Uri] $identityUrl,
        [Parameter(Mandatory=$True)] [string] $apiName,
        [Parameter(Mandatory=$True)] [string] $accessToken
    )

    [Uri]$url = "$($identityUrl.OriginalString)/api/apiresource/$apiName"

    $headers = @{"Accept" = "application/json"}
    if($accessToken){
        $headers.Add("Authorization", "Bearer $accessToken")
    }

    try{
        $clientResponse = Invoke-RestMethod -Method Delete -Uri $url -Headers $headers
        return $clientResponse
    }catch{
        $exception = $_.Exception
        $error = "Unknown error attempting to delete api"
        $exception = $_.Exception
        if ($null -ne $exception -and $null -ne $exception.Response) {
            $error = Get-ErrorFromResponse -response $exception.Response
        }
        throw ( New-Object -TypeName "System.Net.WebException" "There was an error deleting api $apiName with Fabric.Identity: $error, Removing api registration failure.", $exception)   
    }
}

<#
    .Synopsis
    Attempts to edit an api object with fabric identity

    .Description
    Takes in the identity server url the json request body an apiname and an access token.

    .Parameter identityUrl
    The identity url to put the api registration

    .Parameter body
    The json request format for editing the api registration

    .Parameter apiName
    The name of the api

    .Parameter accessToken
    An access token previously retrieved from the identity server

    .Example
    Edit-ApiRegistration -identityUrl "https://server/identity" -body "{"enabled":true, "name": "sample-api", "userClaims":[], "scopes":[]}" -apiName "TestAPI" -accessToken "eyJhbGciO"
#>
function Edit-ApiRegistration {
	param(
        [Parameter(Mandatory=$True)] [Uri] $identityUrl,
        [Parameter(Mandatory=$True)] [string] $body,
        [Parameter(Mandatory=$True)] [string] $apiName,
        [Parameter(Mandatory=$True)] [string] $accessToken
    )

    $url = "$identityUrl/api/apiresource/$apiName"
    $headers = @{"Accept" = "application/json"}
    if($accessToken){
        $headers.Add("Authorization", "Bearer $accessToken")
    }
    try{
        Invoke-RestMethod -Method Put -uri "$url" -body $body -ContentType "application/json" -Headers $headers | out-null

        # Reset api secret
        $apiResponse = Reset-ApiPassword -identityUrl $url -apiName $apiName -accessToken $accessToken
        return $apiResponse.apiSecret
    }catch{
        $exception = $_.Exception
        $apiResourceObject = ConvertFrom-Json -InputObject $body
        $error = "Unknown error attempting to edit api"
        $exception = $_.Exception
        if ($null -ne $exception -and $null -ne $exception.Response) {
            $error = Get-ErrorFromResponse -response $exception.Response
        }
        throw ( New-Object -TypeName "System.Net.WebException" "There was an error editing api $($apiResourceObject.name) with Fabric.Identity: $error, Editing registration failure.", $exception)
    }
}

<#
    .Synopsis
    Attempts to reset an api object password with fabric identity

    .Description
    Takes in the identity server url an apiname and an access token.

    .Parameter identityUrl
    The identity url to reset the api password

    .Parameter apiName
    The name of the api

    .Parameter accessToken
    An access token previously retrieved from the identity server

    .Example
    Reset-ApiPassword -identityUrl "https://server/identity" -apiName "TestAPI" -accessToken "eyJhbGciO"
#>
function Reset-ApiPassword {
	param(
        [Parameter(Mandatory=$True)] [Uri] $identityUrl,
        [Parameter(Mandatory=$True)] [string] $apiName,
        [Parameter(Mandatory=$True)] [string] $accessToken
    )

    [Uri]$url = "$($identityUrl.OriginalString)/api/apiresource/$apiName/resetPassword"

    $headers = @{"Accept" = "application/json"}
    if($accessToken){
        $headers.Add("Authorization", "Bearer $accessToken")
    }
    try{
        # Reset api secret
        $apiResponse = Invoke-RestMethod -Method Post -Uri $url -ContentType "application/json" -Headers $headers
        return $apiResponse.apiSecret
    }catch{
        $exception = $_.Exception
        $error = "Unknown error attempting to reset api"
        $exception = $_.Exception
        if ($null -ne $exception -and $null -ne $exception.Response) {
            $error = Get-ErrorFromResponse -response $exception.Response
        }
        throw ( New-Object -TypeName "System.Net.WebException" "There was an error resetting api password $apiName with Fabric.Identity: $error, Resetting api password failure.", $exception)
    }
}

<#
    .Synopsis
    Attempts to get an api object registration

    .Description
    Takes in the identity server url an apiname and an access token.

    .Parameter identityUrl
    The identity url to get the api object

    .Parameter apiName
    The name of the api

    .Parameter accessToken
    An access token previously retrieved from the identity server

    .Example
    Test-IsApiRegistered -identityUrl "https://server/identity" -apiName "TestAPI" -accessToken "eyJhbGciO"
#>
function Test-IsApiRegistered {
    param(
        [Parameter(Mandatory=$True)] [Uri] $identityUrl,
        [Parameter(Mandatory=$True)] [string] $apiName,
        [Parameter(Mandatory=$True)] [string] $accessToken
    )

    $apiExists = $false
    $url = "$identityUrl/api/apiresource/$apiName"

    $headers = @{"Accept" = "application/json"}
    if($accessToken){
        $headers.Add("Authorization", "Bearer $accessToken")
    }

    try{
        $getResponse = Invoke-RestMethod -Method Get -Uri $url -ContentType "application/json" -Headers $headers
        $apiExists = $true
        return $apiExists
    }catch{
        $exception = $_.Exception
        if ((Assert-WebExceptionType -exception $exception -typeCode 404)) {
            try{
                return $false
            }catch{
                $error = "Unknown error looking for api"
                $exception = $_.Exception
                if ($null -ne $exception -and $null -ne $exception.Response) {
                    $error = Get-ErrorFromResponse -response $exception.Response
                }
                throw (New-Object -TypeName "System.Net.WebException" "There was an error looking for api $($apiResourceObject.name): $error. Registration lookup failure.", $exception)
            }
        }
        else {
            $error = "Unknown error looking for api"
            $exception = $_.Exception
            if ($null -ne $exception -and $null -ne $exception.Response) {
                $error = Get-ErrorFromResponse -response $exception.Response
            }
            throw ( New-Object -TypeName "System.Net.WebException" "There was an error looking for api $($apiResourceObject.name) with Fabric.Identity: $error, Registration lookup failure.", $exception)
        }
    }
}

<#
    .Synopsis
    checks the exception for type

    .Description
    checks in the exception for the typeCode
    Abstracted to facilitate exception inspection
    returns either $true or $false

    .Parameter exception
    an exception or object of some kind thrown

    .Parameter typeCode
    an exception code to find

    .Example
    Assert-WebExceptionType -exception $exception -typeCode 404
#>
function Assert-WebExceptionType( $exception, $typeCode) {
    if ($null -ne $exception -and $exception.Response.StatusCode.value__ -eq $typeCode) {
        return $true
    }
    else {
        return $false
    }
}

<#
    .Synopsis
    INTERNAL: function to extract exception response messages

    .Description
    extracts the exception message if it exists
    Abstracted to facilitate exception inspection
    returns the response body (either an object or a string)

    .Parameter response
    the exception response

    .Example
    Get-ErrorFromResponse -response $exception.response
#>
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
Export-ModuleMember -Function New-ClientCredentialsClientBody
Export-ModuleMember -Function New-ImplicitClientBody
Export-ModuleMember -Function New-HybridClientBody
Export-ModuleMember -Function New-HybridPkceClientBody
Export-ModuleMember -Function Edit-ClientRegistration
Export-ModuleMember -Function Reset-ClientPassword
Export-ModuleMember -Function Test-IsClientRegistered
Export-ModuleMember -Function Get-ApiRegistration
Export-ModuleMember -Function New-ApiRegistration
Export-ModuleMember -Function New-ApiRegistrationBody
Export-ModuleMember -Function Remove-ApiRegistration
Export-ModuleMember -Function Edit-ApiRegistration
Export-ModuleMember -Function Reset-ApiPassword
Export-ModuleMember -Function Test-IsApiRegistered
Export-ModuleMember -Function Assert-WebExceptionType