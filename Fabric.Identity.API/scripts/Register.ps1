#
# Register.ps1
#
function Get-FullyQualifiedHostName(){
    return "https://$env:computername.$($env:userdnsdomain.tolower())"
}
function Get-IdentityServiceUrl(){
    $hostName = Get-FullyQualifiedHostName
    return "$hostName/Identity"
}

function Get-AuthorizationServiceUrl(){
    $hostName = Get-FullyQualifiedHostName
    return "$hostName/Authorization"
}

function Get-DiscoveryServiceUrl(){
    $hostName = Get-FullyQualifiedHostName
    return "$hostName/DiscoveryService/v1"
}

function Get-ApplicationUrl($serviceName, $serviceVersion, $discoveryServiceUrl){
    $discoveryRequest = "$discoveryServiceUrl/Services?`$filter=ServiceName eq '$serviceName' and Version eq $serviceVersion&`$select=ServiceUrl&`$orderby=Version desc"
    $discoveryResponse = Invoke-RestMethod -Method Get -Uri $discoveryRequest -UseDefaultCredentials
    $serviceUrl = $discoveryResponse.value.ServiceUrl
    if([string]::IsNullOrWhiteSpace($serviceUrl)){
	    $addToError = "There was an error getting the service registration for $serviceName, using DiscoveryService url $discoveryRequest."
        throw "The service $serviceName version $serviceVersion and $serviceUrl is not registered with the Discovery service. $addToError Make sure that this version of the service is registered w/ Discovery service before proceeding. Halting installation."
    }
    return $serviceUrl
}

function Get-Headers($accessToken){
    $headers = @{"Accept" = "application/json"}
    if($accessToken){
        $headers.Add("Authorization", "Bearer $accessToken")
    }
    return $headers
}

function Invoke-Get($url, $accessToken)
{
    $headers = Get-Headers -accessToken $accessToken
        
    $getResponse = Invoke-RestMethod -Method Get -Uri $url -Headers $headers
    return $getResponse
}

function Invoke-Put($url, $body, $accessToken)
{
    $headers = Get-Headers -accessToken $accessToken

    if(!($body -is [String])){
        $body = (ConvertTo-Json $body)
    }
    
    $putResponse = Invoke-RestMethod -Method Put -Uri $url -Body $body -ContentType "application/json" -Headers $headers
    Write-Success "    Success."
    Write-Host ""
    return $putResponse
}

function Invoke-Post($url, $body, $accessToken)
{
    $headers = Get-Headers -accessToken $accessToken

    if(!($body -is [String])){
        $body = (ConvertTo-Json $body)
    }
    
    $postResponse = Invoke-RestMethod -Method Post -Uri $url -Body $body -ContentType "application/json" -Headers $headers
    Write-Success "    Success."
    Write-Host ""
    return $postResponse
}

function Get-ErrorFromResponse($response)
{
    $result = $response.GetResponseStream()
    $reader = New-Object System.IO.StreamReader($result)
    $reader.BaseStream.Position = 0
    $reader.DiscardBufferedData()
    $responseBody = $reader.ReadToEnd();
    return $responseBody
}

function Add-AuthorizationRegistration($authUrl, $clientId, $clientName, $accessToken)
{
    $url = "$authUrl/clients"
    $body = @{
        id = "$clientId"
        name = "$clientName"
    }
    try{
        return Invoke-Post $url $body $accessToken
    }catch{
        $exception = $_.Exception
        if($exception -ne $null -and $exception.Response.StatusCode.value__ -eq 409)
        {
            Write-Success "    Client: $clientName has already been registered with Fabric.Authorization"
            Write-Host ""
        }else{
            $error = Get-ErrorFromResponse -response $exception.Response
            Write-Error "    There was an error updating the resource: $error. Halting installation."
            throw $exception
        }
    }
}

function Add-Permission($authUrl, $name, $grain, $securableItem, $accessToken)
{
    $url = "$authUrl/permissions"
    $body = @{
        name = "$name"
        grain = "$grain"
        securableItem = "$securableItem"
    }
    $permission = Invoke-Post $url $body $accessToken
    return $permission
}

function Get-Permission($authUrl, $name, $grain, $securableItem, $accessToken){
    $url = "$authUrl/permissions/$grain/$securableItem/$name"
    $permission = Invoke-Get -url $url -accessToken $accessToken
    return $permission
}

function Invoke-AddOrGetPermission($authUrl, $name, $grain, $securableItem, $accessToken){
    try{
        $permission = Add-Permission -authUrl $authUrl -name $name -grain $grain -securableItem $securableItem -accessToken $accessToken
        return $permission
    }catch{
        $exception = $_.Exception
        if($exception -ne $null -and $exception.Response.StatusCode.value__ -eq 409)
        {
            Write-Success "    Permission: $name has already been created."
            Write-Host ""
            $permission = Get-Permission -authUrl $authUrl -name $name -grain $grain -securableItem $securableItem -accessToken $accessToken
            return $permission
        }else{
            $error = Get-ErrorFromResponse -response $exception.Response
            Write-Error "    There was an error updating the resource: $error. Halting installation."
            throw $exception
        }
    }
}

function Add-Role($authUrl, $name, $displayName, $description, $grain, $securableItem, $accessToken)
{
    $url = "$authUrl/roles"
    $body = @{
        name = "$name"
        grain = "$grain"
        securableItem = "$securableItem"
    }

    if(![string]::IsNullOrWhiteSpace($displayName)){
        $body += @{displayName = $displayName}
    }

    if(![string]::IsNullOrWhiteSpace($description)){
        $body += @{description = $description}
    }

    $role = Invoke-Post $url $body $accessToken
    return $role
}

function Get-Role($authUrl, $name, $grain, $securableItem, $accessToken){
    $url = "$authUrl/roles/$grain/$securableItem/$name"
    $role = Invoke-Get -url $url -accessToken $accessToken
    return $role
}

function Invoke-AddOrGetRole($authUrl, $name, $displayName, $description, $grain, $securableItem, $accessToken){
    try{
        $role = Add-Role -authUrl $authUrl -name $name -displayName $displayName -description $description -grain $grain -securableItem $securableItem -accessToken $accessToken
        return $role
    }catch{
        $exception = $_.Exception
        if($exception -ne $null -and $exception.Response.StatusCode.value__ -eq 409)
        {
            Write-Success "    Role: $name has already been created."
            Write-Host ""
            $role = Get-Role -authUrl $authUrl -name $name -grain $grain -securableItem $securableItem -accessToken $accessToken
            return $role
        }else{
            $error = Get-ErrorFromResponse -response $exception.Response
            Write-Error "    There was an error updating the resource: $error. Halting installation."
            throw $exception
        }
    }
}

function Invoke-AddOrGetGroup($authUrl, $name, $source, $accessToken){
    try{
        Write-Host "    Adding group $name..."
        $group = Add-Group -authUrl $authorizationServiceURL -name $name -source $source -accessToken $accessToken
        return $group
    }catch{
         $exception = $_.Exception
        if($exception -ne $null -and $exception.Response.StatusCode.value__ -eq 409)
        {
            Write-Success "    Group: $name has already been created."
            Write-Host ""
            $group = Get-Group -authUrl $authUrl -name $name -accessToken $accessToken
            return $group
        }else{
            $error = Get-ErrorFromResponse -response $exception.Response
            Write-Error "    There was an error updating the resource: $error. Halting installation."
            throw $exception
        }
    }
}

function Get-Group($authUrl, $name, $accessToken){
    $url = "$authUrl/groups/$name"
    return Invoke-Get -url $url -accessToken $accessToken
}

function Add-Group($authUrl, $name, $displayName, $description, $source, $accessToken)
{
    $url = "$authUrl/groups"
    $body = @{
        groupName = "$name"
        groupSource = "$source"
    }

    if(![string]::IsNullOrWhiteSpace($displayName)){
        $body += @{displayName = $displayName}
    }
        
    if(![string]::IsNullOrWhiteSpace($description)){
        $body += @{description = $description}
    }

    return Invoke-Post $url $body $accessToken
}

function Add-PermissionToRole($authUrl, $roleId, $permission, $accessToken)
{
    $url = "$authUrl/roles/$roleId/permissions"
    $body = @($permission)
    try{
        return Invoke-Post $url $body $accessToken
    }catch{
        $exception = $_.Exception
        if($exception -ne $null -and $exception.Response.StatusCode.value__ -eq 409)
        {
            Write-Success "    Permission: $($permission.name) has already been associated to the role"
            Write-Host ""
        }else{
            $error = Get-ErrorFromResponse -response $exception.Response
            Write-Error "    There was an error updating the resource: $error. Halting installation."
            throw $exception
        }

    }
}

function Add-RoleToGroup($authUrl, $groupName, $role, $accessToken)
{
    $role.permissions = $null
    $encodedGroupName = [System.Web.HttpUtility]::UrlEncode($groupName)
    $url = "$authUrl/groups/$encodedGroupName/roles"
    $body = @($role)
    return Invoke-Post $url $body $accessToken
}

function Add-RoleToGroupSafe($authUrl, $groupName, $role, $accessToken){
    try{
        Write-Host "    Adding Role: $($role.name) to Group $groupName..."
        Add-RoleToGroup -authUrl $authUrl -groupName $groupName -role $role -accessToken $accessToken
    }catch{
        $exception = $_.Exception
        if($exception -ne $null -and $exception.Response -ne $null)
        {
            $error = Get-ErrorFromResponse -response $exception.Response
            if($error.Contains("$($role.id) already exists")){
                Write-Success "    Role: $($role.name) has already been associated to the group"
                Write-Host ""
            }else{
                Write-Error "    There was an error updating the resource: $error. Halting installation."
                throw $exception
            }
        }else{
            Write-Error "    There was an error updating the resource: $exception. Halting installation."
            throw $exception
        }
    }
}

function Test-IsApiRegistered($identityServiceUrl, $apiName, $accessToken){
    $apiExists = $false
    $url = "$identityServiceUrl/api/apiresource/$apiName"

    try{
        $getResponse = Invoke-Get -url $url -accessToken $accessToken
        $apiExists = $true
        return $apiExists
    }catch{
        $exception = $_.Exception
        if($exception -ne $null -and $exception.Response.StatusCode.value__ -ne 404)
        {
            $error = Get-ErrorFromResponse($exception.Response)
            Write-Error $error
            throw $exception
        }
        return $apiExists
    }
}

function Test-IsClientRegistered($identityServiceUrl, $clientId, $accessToken){
    $clientExists = $false
    $url = "$identityServiceUrl/api/client/$clientId"

    try{
        $getResponse = Invoke-Get -url $url -accessToken $accessToken
        $clientExists = $true
        return $clientExists
    }catch{
        $exception = $_.Exception
        if($exception -ne $null -and $exception.Response.StatusCode.value__ -ne 404)
        {
            $error = Get-ErrorFromResponse($exception.Response)
            Write-Error $error
            throw $exception
        }
        return $clientExists
    }
}

function Invoke-UpdateApiRegistration($identityServiceUrl, $body, $apiName, $accessToken){
    $url = "$identityServiceUrl/api/apiresource/$apiName"
    try{
        Invoke-Put -url $url -body $body -accessToken $accessToken
        
        # always reset secret
        Write-Host "    Resetting $apiName API secret"
        $apiResponse = Invoke-Post -url "$url/resetPassword" -accessToken $accessToken
        return $apiResponse.apiSecret
    }catch{
        $exception = $_.Exception
        $error = Get-ErrorFromResponse -response $exception.Response
        Write-Error "There was an error updating the resource: $error. Halting installation."
        throw $exception
    } 
}

function Invoke-UpdateClientRegistration($identityServiceUrl, $body, $clientId, $accessToken){
    $url = "$identityServiceUrl/api/client/$clientId"
    try{
        Invoke-Put -url $url -body $body -accessToken $accessToken

        # always reset secret
        Write-Host "    Resetting $clientId client secret"
        $clientResponse = Invoke-Post -url "$url/resetPassword" -accessToken $accessToken
        return $clientResponse.clientSecret
    }catch{
        $exception = $_.Exception
        $error = Get-ErrorFromResponse -response $exception.Response
        Write-Error "There was an error updating the resource: $error. Halting installation."
        throw $exception
    }
}

function Get-RegistrationSettings()
{
    $registrationConfig = [xml](Get-Content registration.config)
    if($registrationConfig -eq $null){
        Write-Error "There is no configuration defined for the Fabric Registration step."
		throw
    }
    return $registrationConfig.registration
}

function Get-ApiResourcesToRegister($registrationConfig)
{
    if($registrationConfig -eq $null){
        Write-Host "registration config is null"
    }
    $apiResources = $registrationConfig.apiresources
    return $apiResources.api
}

function Get-ClientsToRegister($registrationConfig)
{
    $clients = $registrationConfig.clients
    return $clients.client
}

function Get-RolesAndPermissionsToRegister($registrationConfig){
    $authorization = $registrationConfig.authorization
    return $authorization
}

function Get-ApiResourceFromConfig($api){
    $body = @{
            name = $api.name
            scopes = @()
            userClaims = @()
        }

        foreach($scope in $api.scopes.scope){
            $body.scopes += @{name =$scope }
        }

        foreach($claim in $api.userClaims.claim){
            $body.userClaims += $claim
        }
    return $body
}

function Get-HybridPkceClientFromConfig($client){
    $hybridPkceBody = @{
        requireConsent = $false
        requireClientSecret = $false
        requirePkce = $true
        allowOfflineAccess = $true
        updateAccessTokenClaimsOnRefresh = $true
        redirectUris = @()
    }

    foreach($redirectUri in $client.redirectUris.redirectUri){
        $hybridPkceBody.redirectUris += $redirectUri
    }

    return $hybridPkceBody
}

function Get-ImplicitClientFromConfig($client, $discoveryServiceUrl){
    $implicitBody = @{
        requireConsent = $false
        allowOfflineAccess = $false
        allowAccessTokensViaBrowser = $true
        enableLocalLogin =$false
        accessTokenLifetime = 1200
        allowedCorsOrigins = @()
        redirectUris = @()
        postLogoutRedirectUris = @()
    }
    
    $serviceUrl = Get-ApplicationUrl -serviceName $client.serviceName -serviceVersion $client.serviceVersion -discoveryServiceUrl $discoveryServiceUrl
    $serviceUri = [System.Uri]$serviceUrl
    $scheme = $serviceUri.Scheme
    $path = $serviceUri.AbsolutePath

    $hostnames = Read-Host "    The hostname that will be configured for $($client.serviceName) with Fabric.Identity is $($serviceUri.Host). If there are additional hostnames that need to be set up, please enter a space-separated list now, otherwise press enter to continue";
    $hostnames = $hostnames.trim();
    $hostnames = $hostnames -split '\s+'

    foreach($allowedCorsOrigin in $client.allowedCorsOrigins.corsOrigin){
        $implicitBody.allowedCorsOrigins += [string]::Format($allowedCorsOrigin, $serviceUrl)
        foreach($hostname in $hostnames){
            if(![System.String]::IsNullOrWhiteSpace($hostname)){
                $alternateUrl = [string]::Format("{0}://{1}{2}", $scheme, $hostname, $path)
                $implicitBody.allowedCorsOrigins += [string]::Format($allowedCorsOrigin, $alternateUrl)
            }
        }
    }

    foreach($redirectUri in $client.redirectUris.redirectUri){
        $implicitBody.redirectUris += [string]::Format($redirectUri, $serviceUrl)
        foreach($hostname in $hostnames){
            if(![System.String]::IsNullOrWhiteSpace($hostname)){
                $alternateUrl = [string]::Format("{0}://{1}{2}", $scheme, $hostname, $path)
                $implicitBody.redirectUris += [string]::Format($redirectUri, $alternateUrl)
            }
        }
    }

    foreach($redirectUri in $client.postLogoutRedirectUris.redirectUri){
        $implicitBody.postLogoutRedirectUris += [string]::Format($redirectUri, $serviceUrl)
        foreach($hostname in $hostnames){
            if(![System.String]::IsNullOrWhiteSpace($hostname)){
                $alternateUrl = [string]::Format("{0}://{1}{2}", $scheme, $hostname, $path)
                $implicitBody.postLogoutRedirectUris += [string]::Format($redirectUri, $alternateUrl)
            }
        }
    }
    
    return $implicitBody
}

function Get-ClientFromConfig($client, $discoveryServiceUrl){
    $body = @{
            clientId = $client.clientid
            clientName = $client.clientName
            allowedScopes = @()
            allowedGrantTypes = @()
        }
    
    foreach($scope in $client.allowedScopes.scope){
        $body.allowedScopes += $scope
    }

    foreach($grantType in $client.allowedGrantTypes.grantType){
        $body.allowedGrantTypes += $grantType
    }

    if($body.allowedGrantTypes.Contains("implicit")){
        $implicitBody = Get-ImplicitClientFromConfig -client $client -body $body -discoveryServiceUrl $discoveryServiceUrl
        $body = $implicitBody + $body
    }elseif ($body.allowedGrantTypes.Contains("hybrid") -and $client.requirePkce -eq $true) {
        $hybridPkceBody = Get-HybridPkceClientFromConfig -client $client
        $body = $hybridPkceBody + $body
	}

    return $body
}

function Get-WebConfigPath($service, $discoveryServiceUrl){
    $serviceUrl = Get-ApplicationUrl -serviceName $service.serviceName -serviceVersion $service.serviceVersion -discoveryServiceUrl $discoveryServiceUrl

    if([string]::IsNullOrWhiteSpace($serviceUrl)){
        throw "Could not retrieve a registered URL from DiscoveryService for $($service.serviceName). Halting installation."
    }

    $serviceUri = [System.Uri]$serviceUrl

	$serviceAbsolutePath = $serviceUri.AbsolutePath
	$versionSuffix = "/v$($service.serviceVersion)"

	if($serviceAbsolutePath.EndsWith($versionSuffix)){
		$serviceAbsolutePath = $serviceAbsolutePath.Substring(0, $serviceAbsolutePath.Length-$versionSuffix.Length)
	}

    $app = Get-WebApplication | Where-Object {$_.Path -eq $serviceAbsolutePath}

    if($app -eq $null){
        throw "Could not find an installed application that matches the path of the registered URL: $serviceUrl. Halting installation."
    }

    $appPath = [Environment]::ExpandEnvironmentVariables($app.PhysicalPath)

    $configPath = [System.IO.Path]::Combine($appPath, "web.config")
    
    if(!(Test-Path $configPath)){
        throw "Could not find a web.config in $appPath for the $($app.Path) application. Halting installation."
    }

    return $configPath
}

function Invoke-WriteSecretToConfig($service, $secret, $configPath){
    
    if(!([string]::IsNullOrWhiteSpace($service.secretConfig))){
        Add-WebConfigAppSetting -webConfigLocation $configPath -settingKey $service.secretConfig -settingValue $secret | Out-Null
        Write-Success "    Wrote secret to $($service.serviceName) configuration file located at $configPath (key=$($service.secretConfig), value=$secret)."
        Write-Host ""
    }
}

function Add-WebConfigAppSetting($webConfigLocation, $settingKey, $settingValue) {
    $webConfigDoc = [xml](Get-Content $webConfigLocation)
    $appSettings = $webConfigDoc.configuration.appSettings
    
    $existingSetting = $appSettings.add | where {$_.key -eq $settingKey}
    
    if ($existingSetting -eq $null) {
        $setting = $webConfigDoc.CreateElement("add")

        $keyAttribute = $webConfigDoc.CreateAttribute("key")
        $keyAttribute.Value = $settingKey;
        $setting.Attributes.Append($keyAttribute)

        $valueAttribute = $webConfigDoc.CreateAttribute("value")
        $valueAttribute.Value = $settingValue
        $setting.Attributes.Append($valueAttribute)

        $appSettings.AppendChild($setting)
    }
    else {
        $existingSetting.Value = $settingValue
    }

    $webConfigDoc.Save($webConfigLocation)
}

function Invoke-RegisterApiResources($apiResources, $identityServiceUrl, $accessToken, $discoveryServiceUrl)
{
    Write-Host "Registering APIs..."
    Write-Host ""
    if($apiResources -eq $null){
        Write-Host "    No API resources"
    }
    foreach($api in $apiResources){
		try{
            [string]$apiSecret = [string]::Empty
			Write-Host "    Registering $($api.name) with Fabric.Identity"
			$body = Get-ApiResourceFromConfig($api)
			$isApiRegistered = Test-IsApiRegistered -identityServiceUrl $identityServiceUrl -apiName $api.name -accessToken $accessToken

			if($isApiRegistered){
				Write-Host "    $($api.name) is already registered, updating"
				$apiSecret = Invoke-UpdateApiRegistration -identityServiceUrl $identityServiceUrl -body $body -apiName $api.name -accessToken $accessToken
			}else{
				$apiSecret = Add-ApiRegistration -authUrl $identityServiceUrl -body (ConvertTo-Json $body) -accessToken $accessToken
            }
            
            if (![string]::IsNullOrEmpty($apiSecret) -and ![string]::IsNullOrWhiteSpace($apiSecret)) {
                $configPath = Get-WebConfigPath -service $api -discoveryServiceUrl $discoveryServiceUrl
                Invoke-WriteSecretToConfig -service $api -secret $apiSecret.Trim() -configPath $configPath
            }
		}
		catch{
			Write-Error "Could not register api $($api.name)"
			$exception = $_.Exception
			Write-Error $exception
			throw $exception
		}
    }
}

function Invoke-RegisterClients($clients, $identityServiceUrl, $accessToken, $discoveryServiceUrl)
{
    Write-Host "Registering Clients..."
    Write-Host ""
    if($clients -eq $null){
        Write-Host "    No Clients to register"
    }
    foreach($client in $clients){
		try{
            [string]$clientSecret = [string]::Empty
			Write-Host "    Registering $($client.clientid) with Fabric.Identity"
			$body = Get-ClientFromConfig -client $client -discoveryServiceUrl $discoveryServiceUrl
			$isClientRegistered = Test-IsClientRegistered -identityServiceUrl $identityServiceUrl -clientId $client.clientid -accessToken $accessToken

			if($isClientRegistered){
				Write-Host "    $($client.clientid) is already registered, updating"
				$clientSecret = Invoke-UpdateClientRegistration -identityServiceUrl $identityServiceUrl -body $body -clientId $client.clientid -accessToken $accessToken
			}else{
				$jsonBody = ConvertTo-Json $body
				$clientSecret = Add-ClientRegistration -authUrl $identityServiceUrl -body $jsonBody -accessToken $accessToken 
            }

			if (![string]::IsNullOrEmpty($clientSecret) -and ![string]::IsNullOrWhiteSpace($clientSecret) -and !$client.allowedGrantTypes.grantType.Contains("hybrid") -and ($client.requirePkce -eq $null -or ![boolean]$client.requirePkce)) {
                $configPath = Get-WebConfigPath -service $client -discoveryServiceUrl $discoveryServiceUrl
                Invoke-WriteSecretToConfig -service $client -secret $clientSecret.Trim() -configPath $configPath
			}

			Write-Host "    Registering $($client.clientid) with Fabric.Authorization"
			$authorizationClient = Add-AuthorizationRegistration -authUrl $authorizationServiceURL -clientId $client.clientid -clientName $client.clientName -accessToken $accessToken

			if($client.authorization -ne $null){
				Invoke-RegisterRolesAndPermissions -grainName "app" -securableItemName $client.clientid -securableItem $client.authorization -identityServiceUrl $identityServiceUrl -accessToken $accessToken
			}
		}
		catch{
			Write-Error "Could not register client $($client.clientid)"
			$exception = $_.Exception
			Write-Error $exception
			throw $exception
		}
    }
}

function Invoke-RegisterSharedRolesAndPermissions($rolesAndPermissions, $identityServiceUrl, $accessToken){
    Write-Host "Creating Roles and Permissions..."
    Write-Host ""
    if($rolesAndPermissions -eq $null){
        Write-Host "    No roles and permissions to register"
    }
    foreach($grain in $rolesAndPermissions.grain){
        $grainName = $grain.name
        foreach($securableItem in $grain.securableItem){
            $securableItemName = $securableItem.name
            Invoke-RegisterRolesAndPermissions -grainName $grainName -securableItemName $securableItemName -securableItem $securableItem -identityServiceUrl $identityServiceUrl -accessToken $accessToken
        }
    }
}

function Invoke-RegisterRolesAndPermissions($grainName, $securableItemName, $securableItem, $identityServiceUrl, $accessToken){
    foreach($role in $securableItem.role){
        $roleName = $role.name
        $roleDisplayName = $role.displayName
        $roleDescription = $role.description
        Write-Host "    Adding role: $roleName for grain: $grainName and securableItem: $securableItemName"
        $addedRole = Invoke-AddOrGetRole -authUrl $authorizationServiceURL -name $roleName -displayName $roleDisplayName -description $roleDescription -grain $grainName -securableItem $securableItemName -accessToken $accessToken
        if(!([string]::IsNullOrEmpty($($role.groupName)))){
            $group = Invoke-AddOrGetGroup -authUrl $authorizationServiceURL -name $role.groupName -source "custom" -accessToken $accessToken
            Add-RoleToGroupSafe -authUrl $authorizationServiceURL -groupName $role.groupName -role $addedRole -accessToken $accessToken
        }
        foreach($permission in $role.permission){
            $permissionName = $permission.name
            Write-Host "    Adding permission: $permissionName for grain: $grainName and securableItem: $securableItemName"
            $addedPermission = Invoke-AddOrGetPermission -authUrl $authorizationServiceURL -name $permissionName -grain $grainName -securableItem $securableItemName -accessToken $accessToken
            Write-Host "    Associating permission: $permissionName with role: $roleName"
            $rolePermission = Add-PermissionToRole -authUrl $authorizationServiceURL -roleId $addedRole.id -permission $addedPermission -accessToken $accessToken
        }
    }
}

if(!(Test-Path .\Fabric-Install-Utilities.psm1)){
	Invoke-WebRequest -Uri https://raw.githubusercontent.com/HealthCatalyst/InstallScripts/master/common/Fabric-Install-Utilities.psm1 -OutFile Fabric-Install-Utilities.psm1
}
Import-Module -Name .\Fabric-Install-Utilities.psm1 -Force

if(!(Test-IsRunAsAdministrator))
{
    Write-Error "You must run this script as an administrator. Halting configuration." -ErrorAction Stop
}

$installSettings = Get-InstallationSettings "identity"
$fabricInstallerSecret = $installSettings.fabricInstallerSecret
$discoveryServiceUrl = $installSettings.discoveryService
$authorizationServiceURL =  $installSettings.authorizationService
$identityServiceUrl = $installSettings.identityService
$encryptionCertificateThumbprint = $installSettings.encryptionCertificateThumbprint

if([string]::IsNullOrEmpty($installSettings.identityService))  
{
	$identityServiceUrl = Get-IdentityServiceUrl
} else
{
	$identityServiceUrl = $installSettings.identityService
}

if([string]::IsNullOrEmpty($installSettings.authorizationService))  
{
	$authorizationServiceURL = Get-AuthorizationServiceUrl
} else
{
	$authorizationServiceURL = $installSettings.authorizationService
}

if([string]::IsNullOrEmpty($installSettings.discoveryService))  
{
	$discoveryServiceUrl = Get-DiscoveryServiceUrl
} else
{
	$discoveryServiceUrl = $installSettings.discoveryService
	$discoveryServiceUrl = $discoveryServiceUrl.TrimEnd("/")
    if ($discoveryServiceUrl -notmatch "/v\d")
	{
	  $discoveryServiceUrl = $discoveryServiceUrl + "/v1"
	}
}

try{
	$encryptionCert = Get-Certificate $encryptionCertificateThumbprint
}catch{
	Write-Host "Could not get encryption certificate with thumbprint $encryptionCertificateThumbprint. Please verify that the encryptionCertificateThumbprint setting in install.config contains a valid thumbprint for a certificate in the Local Machine Personal store. Halting installation."
	throw $_.Exception
}

$fabricInstallerSecret = Read-FabricInstallerSecret($fabricInstallerSecret)

$userEnteredDiscoveryServiceUrl = Read-Host  "Enter the URL for the Discovery Service or hit enter to accept the default [$discoveryServiceUrl]"
Write-Host ""
if(![string]::IsNullOrEmpty($userEnteredDiscoveryServiceUrl)){   
     $discoveryServiceUrl = $userEnteredDiscoveryServiceUrl
}

$userEnteredAuthorizationServiceURL = Read-Host  "Enter the URL for the Authorization Service or hit enter to accept the default [$authorizationServiceURL]"
Write-Host ""
if(![string]::IsNullOrEmpty($userEnteredAuthorizationServiceURL)){   
     $authorizationServiceURL = $userEnteredAuthorizationServiceURL
}

$userEnteredIdentityServiceURL = Read-Host  "Enter the URL for the Identity Service or hit enter to accept the default [$identityServiceUrl]"
Write-Host ""
if(![string]::IsNullOrEmpty($userEnteredIdentityServiceURL)){   
     $identityServiceUrl = $userEnteredIdentityServiceURL
}

if([string]::IsNullOrWhiteSpace($fabricInstallerSecret))
{
    Write-Error "You must enter a value for the installer secret" -ErrorAction Stop
}
if([string]::IsNullOrWhiteSpace($authorizationServiceURL))
{
    Write-Error "You must enter a value for the Fabric.Authorization URL" -ErrorAction Stop
}
if([string]::IsNullOrWhiteSpace($identityServiceUrl))
{
    Write-Error "You must enter a value for the Fabric.Identity URL." -ErrorAction Stop
}
if([string]::IsNullOrWhiteSpace($discoveryServiceUrl))
{
    Write-Error "You must enter a value for the Discovery Service URL." -ErrorAction Stop
}

if([string]::IsNullOrWhiteSpace($encryptionCertificateThumbprint))
{
    Write-Error "There is no encryption certificate thumb-print stored in the install.config, halting installation." -ErrorAction Stop
}

# Get the installer access token
$accessToken = Get-AccessToken $identityServiceUrl "fabric-installer" "fabric/identity.manageresources fabric/authorization.write fabric/authorization.read fabric/authorization.dos.write fabric/authorization.manageclients" $fabricInstallerSecret

# Read the registration.config file
$registrationSettings = Get-RegistrationSettings

# Register API Resources specified in the registration.config file
$apiResources = Get-ApiResourcesToRegister -registrationConfig  $registrationSettings
Invoke-RegisterApiResources -apiResources $apiResources -identityServiceUrl $identityServiceUrl -accessToken $accessToken -discoveryServiceUrl $discoveryServiceUrl

# Register the Clients specified in the registration.config file
$clients = Get-ClientsToRegister -registrationConfig $registrationSettings
Invoke-RegisterClients -clients $clients -identityServiceUrl $identityServiceUrl -accessToken $accessToken -discoveryServiceUrl $discoveryServiceUrl

# Register shared roles and permissions
$authorization = Get-RolesAndPermissionsToRegister -registrationConfig $registrationSettings
Invoke-RegisterSharedRolesAndPermissions -rolesAndPermissions $authorization -identityServiceUrl $identityServiceUrl -accessToken $accessToken

Read-Host -Prompt "Registration complete, press Enter to exit"