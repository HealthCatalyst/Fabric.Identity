#
# Install_Identity_Dev.ps1
#
param([String] $couchDbUsername = "admin",
      [String] $couchDbPassword = "admin") 

function Test-RegistrationComplete($authUrl)
{
    $url = "$authUrl/api/client/fabric-installer"
    $headers = @{"Accept" = "application/json"}
    
    try {
        Invoke-RestMethod -Method Get -Uri $url -Headers $headers
    } catch {
        $exception = $_.Exception
    }

    if($exception -ne $null -and $exception.Response.StatusCode.value__ -eq 401)
    {
        Write-Host "Fabric registration is already complete."
        return $true
    }

    return $false
}

function Create-CouchDb($couchDbUsername, $couchDbPassword, $couchDbServer)
{
    try	{
        
		Write-Host "Installing CouchDB via docker..."
        docker stop fabric.couchdb
        docker rm fabric.couchdb
        docker volume rm couchdb-data
        docker run -d --name fabric.couchdb -e "COUCHDB_USER=$couchDbUsername" -e "COUCHDB_PASSWORD=$couchDbPassword" -v couchdb-data:/opt/couchdb/data -p 0.0.0.0:5984:5984 healthcatalyst/fabric.docker.couchdb

    } catch [System.Management.Automation.CommandNotFoundException]{
		
        $originalProgressPreference = $progressPreference
		try{
			Write-Host "Docker not installed, downloading and installing CouchDB for Windows..."
			$progressPreference = 'silentlyContinue'
			Invoke-WebRequest -Uri https://dl.bintray.com/apache/couchdb/win/2.1.0/apache-couchdb-2.1.0.msi -OutFile $env:Temp\apache-couchdb-2.1.0.msi -UseBasicParsing
			Write-Host "Launching CouchDB interactive installation..."
			Start-Process $env:Temp\apache-couchdb-2.1.0.msi -Wait
		}catch{
			Write-Error "Could not download and launch CouchDB installer. Please install CouchDB manually: https://dl.bintray.com/apache/couchdb/win/2.1.0/apache-couchdb-2.1.0.msi. Halting installation."
			throw
		}finally{
			$progressPreference = $originalProgressPreference
			Remove-Item $env:Temp\apache-couchdb-2.1.0.msi
		}

        try{
            Invoke-RestMethod -Method Put -Uri "$couchDbServer/_node/couchdb@localhost/_config/admins/$couchDbUsername" -Body "`"$couchDbPassword`""
        } catch{
            $exception = $_.Exception
            Write-Error "Failed to create admin user for CouchDB. Exception $exception. Please create and admin user for CouchDB and run the script again."
			throw
        }

    } catch{
		Write-Error "Could not install CouchDB via docker. Please ensure docker is running and re-run the installation script."
		throw
	}
}

function Invoke-InstallHostingComponents()
{
	
    if(!(Test-Prerequisite '*.NET Core*Windows Server Hosting*' 1.1.30327.81))
    {
		$originalProgressPreference = $progressPreference
		try{
			Write-Host "Windows Server Hosting Bundle minimum version 1.1.30327.81 not installed...installing version 1.1.30327.81"
			$progressPreference = 'silentlyContinue'
			Invoke-WebRequest -Uri https://go.microsoft.com/fwlink/?linkid=844461 -OutFile $env:Temp\bundle.exe -UseBasicParsing
			Start-Process $env:Temp\bundle.exe -Wait -ArgumentList '/quiet /install'
			net stop was /y
			net start w3svc
			Remove-Item $env:Temp\bundle.exe
		}catch{
			Write-Error "Could not install .NET Windows Server Hosting bundle is installed. Please install the hosting bundle before proceeding. https://go.microsoft.com/fwlink/?linkid=844461" -ErrorAction Stop
		}finally{
			$progressPreference = $originalProgressPreference
        }
    }else{
        Write-Host ".NET Core Windows Server Hosting Bundle installed and meets expectations."
    }
}

function Invoke-InstallCouchDb($couchDbServer, $couchDbUsername, $couchDbPassword)
{
    if(!(Test-Prerequisite '*CouchDB*'))
    {
        Write-Host "CouchDB not installed locally, testing to see if is installed on a remote server using $couchDbServer"
        $remoteInstallationStatus = Get-CouchDbRemoteInstallationStatus $couchDbServer 2.0.0
        if($remoteInstallationStatus -eq "NotInstalled")
        {
            Write-Host "CouchDB not installed, attempting to install couchdb..."
            try{
                Create-CouchDb $couchDbUsername $couchDbPassword $couchDbServer
            } catch{
                $exception = $_.Exception
                Write-Error "Failed to create CouchDB. Exception $exception" -ErrorAction Stop
            }
        }elseif($remoteInstallationStatus -eq "MinVersionNotMet"){
            Write-Host "CouchDB is installed on $couchDbServer but does not meet the minimum version requirements, you must have CouchDB 2.0.0.1 or greater installed: https://dl.bintray.com/apache/couchdb/win/2.1.0/apache-couchdb-2.1.0.msi. Halting installation."
            exit 1
        }else{
            Write-Host "CouchDB installed and meets specifications"
        }
    }elseif (!(Test-Prerequisite '*CouchDB*' 2.0.0.1)) {
        Write-Host "CouchDB is installed but does not meet the minimum version requirements, you must have CouchDB 2.0.0.1 or greater installed: https://dl.bintray.com/apache/couchdb/win/2.1.0/apache-couchdb-2.1.0.msi. Halting installation."
        exit 1
    }else{
        Write-Host "CouchDB installed and meets specifications"
    }
}

function Invoke-RegisterIdentityApi($identityServerUrl)
{
    #Register registration api
$body = @'
{
    "name":"registration-api",
    "userClaims":["name","email","role","groups"],
    "scopes":[{"name":"fabric/identity.manageresources"}, {"name":"fabric/identity.read"}, {"name":"fabric/identity.searchusers"}]
}
'@

    Write-Host "Registering Fabric.Identity registration api."
	try{
		$registrationApiSecret = Add-ApiRegistration -authUrl $identityServerUrl -body $body
	}catch{
		$exception = $_.Exception
		$responseCode = $exception.Response.StatusCode.value__
		Write-Error "Error registering IdentityApi: $exception. Received response code: $responseCode. Halting installation." -ErrorAction Stop
	}
}

function Invoke-RegisterFabricInstallerClient($couchDbServer, $couchDbUsername, $couchDbPassword)
{
    #Register Fabric.Installer
$body = @'
{
  "Enabled": true,
  "ClientId": "fabric-installer",
  "ProtocolType": "oidc",
  "ClientSecrets": [
    {
      "Description": null,
      "Value": "xUNCqCJTNeM3NDd3fH0LGIOftRHrcaHB3DwRQACZdYA=",
      "Expiration": null,
      "Type": "SharedSecret"
    }
  ],
  "RequireClientSecret": true,
  "ClientName": "Fabric Installer",
  "ClientUri": null,
  "LogoUri": null,
  "RequireConsent": false,
  "AllowRememberConsent": true,
  "AllowedGrantTypes": [
    "client_credentials"
  ],
  "RequirePkce": false,
  "AllowPlainTextPkce": false,
  "AllowAccessTokensViaBrowser": false,
  "RedirectUris": [],
  "PostLogoutRedirectUris": [],
  "LogoutUri": null,
  "LogoutSessionRequired": true,
  "AllowOfflineAccess": false,
  "AllowedScopes": [
    "fabric/identity.manageresources",
    "fabric/authorization.read",
    "fabric/authorization.write",
    "fabric/authorization.manageclients"
  ],
  "AlwaysIncludeUserClaimsInIdToken": false,
  "IdentityTokenLifetime": 300,
  "AccessTokenLifetime": 3600,
  "AuthorizationCodeLifetime": 300,
  "AbsoluteRefreshTokenLifetime": 2592000,
  "SlidingRefreshTokenLifetime": 1296000,
  "RefreshTokenUsage": 1,
  "UpdateAccessTokenClaimsOnRefresh": false,
  "RefreshTokenExpiration": 1,
  "AccessTokenType": 0,
  "EnableLocalLogin": true,
  "IdentityProviderRestrictions": [],
  "IncludeJwtId": false,
  "Claims": [],
  "AlwaysSendClientClaims": false,
  "PrefixClientClaims": true,
  "AllowedCorsOrigins": []
}
'@

    $headers = @{"Accept" = "application/json"}
    $credentials = [System.Convert]::ToBase64String([System.Text.Encoding]::UTF8.GetBytes("$($couchDbUserName):$($couchDbPassword)"))
    $headers.Add("Authorization", "Basic $credentials")

    Write-Host "Registering Fabric.Installer."
	try{
		$response = Invoke-RestMethod -Method Put -Uri "$couchDbServer/identity/client%3Afabric-installer" -Body $body -ContentType "application/json" -Headers $headers
	}catch{
		$exception = $_.Exception
		$responseCode = $exception.Response.StatusCode.value__
		Write-Error "Error registering Fabric-Installer client: $exception. Received response code: $responseCode. Halting installation." -ErrorAction Stop
	}
}

function Invoke-PublishIdentity($webroot, $appName, $iisUser, $siteName)
{
	try{
		$appDirectory = "$webroot\$appName"
		New-AppRoot $appDirectory $iisUser
		Write-Host "App directory is: $appDirectory"
		New-AppPool $appName
		New-App $appName $siteName $appDirectory
		try{
			Stop-WebAppPool -Name $appName -ErrorAction Stop
		}catch [System.InvalidOperationException]{
			Write-Host "AppPool $appName is already stopped, continuing."
		}
		dotnet restore ..\Fabric.Identity.API.csproj
		dotnet build ..\Fabric.Identity.API.csproj
		dotnet publish ..\Fabric.Identity.API.csproj -c Debug -o $appDirectory

		Start-WebAppPool -Name $appName
	}catch{
		$exception = $_.Exception
		Write-Error "Could not publish Fabric.Identity. Exception $exception. Halting installation."
	}
}

function Invoke-WriteConfig($couchDbUsername, $couchDbPassword, $hostUrl, $webroot, $appName)
{
    Write-Host "Loading up environment variables..."
    $environmentVariables = @{"HostingOptions__StorageProvider" = "CouchDb"; "HostingOptions__UseTestUsers" = "false"; "AllowLocalLogin" = "false"}


    if ($couchDbUsername){
        $environmentVariables.Add("CouchDbSettings__Username", $couchDbUsername)
    }

    if ($couchDbPassword){
        $environmentVariables.Add("CouchDbSettings__Password", $couchDbPassword)
    }

    $environmentVariables.Add("IdentityServerConfidentialClientSettings__Authority", "${hostUrl}/${appName}")

    $appDirectory = "$($webroot)\$($appName)"
	try{
		Set-EnvironmentVariables $appDirectory $environmentVariables
	}catch{
		$exception = $_.Exception
		Write-Error "Could not write Fabric.Identity configuration. Exception: $exception. Halting installation."
	}
}

function Add-DiscoveryRegistration($identityServerUrl)
{
    Write-Host "Creating IdentityService registration in Discovery..."
    $insertStatement = "
    IF NOT EXISTS
    (
        SELECT *
        FROM [CatalystAdmin].[DiscoveryServiceBASE]
        WHERE [ServiceNM] = N'IdentityService'
    )
        BEGIN
            INSERT INTO [CatalystAdmin].[DiscoveryServiceBASE]
            ([ServiceNM],
             [ServiceUrl],
             [ServiceVersion],
             [DiscoveryTypeCD],
             [HiddenFLG],
             [FriendlyNM]
            )
            VALUES
            (N'IdentityService',
             N'$identityServerUrl',
             1,
             N'Service',
             0,
             N'IdentityService'
            );
    END; 
    "
    try{
        $result = Invoke-Sqlcmd -Query $insertStatement -ServerInstance "." -Database "EDWAdmin"
		Write-Host "$result"
    }catch {
        Write-Warning "Could not register Identity with Discovery service due to the above error."
        Write-Warning "Apps that depend on finding Identity in DiscoveryService (like Atlas) may not work properly. Continuing..."
    }
}

function Get-AccessTokenForInstaller($identityServerUrl)
{
	try{
		$accessToken = Get-AccessToken -authUrl $identityServerUrl -clientId "fabric-installer" -scope "fabric/identity.manageresources" -secret "installer-secret"
		return $accessToken
	} catch {
		Write-Host "There was a problem getting an access token for the Fabric Installer client, please make sure that Fabric.Identity is running and that the fabricInstallerSecret value in the install.config is correct. Halting installation."
		throw $_.Exception
		exit 1
	}
}

function Invoke-RegisterFabricAuthorizationApi($identityServerUrl, $accessToken)
{
	#Register authorization api
	$body = @'
	{
		"name":"authorization-api",
		"userClaims":["name","email","role","groups"],
		"scopes":[{"name":"fabric/authorization.read"}, {"name":"fabric/authorization.write"}, {"name":"fabric/authorization.manageclients"}]
	}
'@

	Write-Host "Registering Fabric.Authorization API."
	try {
		$authorizationApiSecret = Add-ApiRegistration -authUrl $identityServerUrl -body $body -accessToken $accessToken
		Write-Host ""
	} catch {
		Write-Host "Fabric.Authorization API is already registered."
		Write-Host ""
	}
}

if(!(Test-Path .\Fabric-Install-Utilities.psm1)){
    try { 
        $progressPreference = 'silentlyContinue'
        Invoke-WebRequest -Uri https://raw.githubusercontent.com/HealthCatalyst/InstallScripts/master/common/Fabric-Install-Utilities.psm1 -Headers @{"Cache-Control"="no-cache"} -OutFile Fabric-Install-Utilities.psm1 -UseBasicParsing
    } finally { $progressPreference = $originalProgressPreference }
}
Import-Module -Name .\Fabric-Install-Utilities.psm1 -Force

$installSettings = Get-InstallationSettings "identity"

$workingDirectory = Get-CurrentScriptDirectory

Invoke-InstallHostingComponents
Invoke-InstallCouchDb $installSettings.couchDbServer $couchDbUsername $couchDbPassword
Invoke-PublishIdentity $installSettings.webroot $installSettings.appName $installSettings.iisUser $installSettings.siteName
Invoke-WriteConfig $couchDbUsername $couchDbPassword $installSettings.hostUrl $installSettings.webroot $installSettings.appName


Set-Location $workingDirectory
$identityServerUrl = "$($installSettings.hostUrl)/$($installSettings.appName)"

Add-DiscoveryRegistration $identityServerUrl

if(Test-RegistrationComplete $identityServerUrl)
{
    Write-Host "Installation complete, exiting."
    exit 0
}

Invoke-RegisterIdentityApi $identityServerUrl
Invoke-RegisterFabricInstallerClient $installSettings.couchDbServer $couchDbUsername $couchDbPassword
$accessToken = Get-AccessTokenForInstaller $identityServerUrl
Invoke-RegisterFabricAuthorizationApi $identityServerUrl $accessToken

Write-Host "Installation complete, exiting."