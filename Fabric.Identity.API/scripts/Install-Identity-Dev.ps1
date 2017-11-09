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
		Write-Host "Docker not installed, downloading and installing CouchDB for Windows..."
		Invoke-WebRequest -Uri https://dl.bintray.com/apache/couchdb/win/2.1.0/apache-couchdb-2.1.0.msi -OutFile $env:Temp\apache-couchdb-2.1.0.msi
        Write-Host "Launching CouchDB interactive installation..."
        Start-Process $env:Temp\apache-couchdb-2.1.0.msi -Wait
        Remove-Item $env:Temp\apache-couchdb-2.1.0.msi
        try{
            Invoke-RestMethod -Method Put -Uri "$couchDbServer/_node/couchdb@localhost/_config/admins/$couchDbUsername" -Body "`"$couchDbPassword`""
        } catch{
            $exception = $_.Exception
            Write-Host "Failed to create admin user for CouchDB. Exception $exception"
			throw
        }
	}
}

if(!(Test-Path .\Fabric-Install-Utilities.psm1)){
	Invoke-WebRequest -Uri https://raw.githubusercontent.com/HealthCatalyst/InstallScripts/master/common/Fabric-Install-Utilities.psm1 -Headers @{"Cache-Control"="no-cache"} -OutFile Fabric-Install-Utilities.psm1
}
Import-Module -Name .\Fabric-Install-Utilities.psm1 -Force

$installSettings = Get-InstallationSettings "identity"
$zipPackage = $installSettings.zipPackage
$webroot = $installSettings.webroot
$appName = $installSettings.appName
$iisUser = $installSettings.iisUser
$primarySigningCertificateThumbprint = $installSettings.primarySigningCertificateThumbprint -replace '[^a-zA-Z0-9]', ''
$encryptionCertificateThumbprint = $installSettings.encryptionCertificateThumbprint -replace '[^a-zA-Z0-9]', ''
$couchDbServer = $installSettings.couchDbServer
$appInsightsInstrumentationKey = $installSettings.appInsightsInstrumentationKey
$siteName = $installSettings.siteName
$hostUrl = $installSettings.hostUrl
$ldapServer = $installSettings.ldapServer
$ldapPort = $installSettings.ldapPort
$ldapUserName = $installSettings.ldapUserName
$ldapPassword = $installSettings.ldapPassword
$ldapUseSsl = $installSettings.ldapUseSsl
$ldapBaseDn = $installSettings.ldapBaseDn

$workingDirectory = Get-CurrentScriptDirectory

if(!(Test-Prerequisite '*.NET Core*Windows Server Hosting*' 1.1.30327.81))
{
     Write-Host "Windows Server Hosting Bundle minimum version 1.1.30327.81 not installed...installing version 1.1.30327.81"
     Invoke-WebRequest -Uri https://go.microsoft.com/fwlink/?linkid=844461 -OutFile $env:Temp\bundle.exe
     Start-Process $env:Temp\bundle.exe -Wait -ArgumentList '/quiet /install'
     net stop was /y
     net start w3svc
     Remove-Item $env:Temp\bundle.exe
}else{
    Write-Host ".NET Core Windows Server Hosting Bundle installed and meets expectations."
}

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


#Write environment variables
Write-Host "Loading up environment variables..."
$environmentVariables = @{"HostingOptions__StorageProvider" = "CouchDb"; "HostingOptions__UseTestUsers" = "false"; "AllowLocalLogin" = "false"}


if ($couchDbUsername){
	$environmentVariables.Add("CouchDbSettings__Username", $couchDbUsername)
}

if ($couchDbPassword){
	$environmentVariables.Add("CouchDbSettings__Password", $couchDbPassword)
}

$environmentVariables.Add("IdentityServerConfidentialClientSettings__Authority", "${hostUrl}/${appName}")

Set-EnvironmentVariables $appDirectory $environmentVariables

Set-Location $workingDirectory
$identityServerUrl = "$hostUrl/identity"

if(Test-RegistrationComplete $identityServerUrl)
{
    Write-Host "Installation complete, exiting."
    exit 0
}

#Register registration api
$body = @'
{
    "name":"registration-api",
    "userClaims":["name","email","role","groups"],
    "scopes":[{"name":"fabric/identity.manageresources"}, {"name":"fabric/identity.read"}, {"name":"fabric/identity.searchusers"}]
}
'@

Write-Host "Registering Fabric.Identity registration api."
$registrationApiSecret = Add-ApiRegistration -authUrl $identityServerUrl -body $body

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
Write-Host $headers
Write-Host "Client URL: $couchDbServer"
$response = Invoke-RestMethod -Method Put -Uri "$couchDbServer/identity/client%3Afabric-installer" -Body $body -ContentType "application/json" -Headers $headers

Write-Host "Installation complete, exiting."