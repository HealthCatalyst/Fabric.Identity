#
# Install_Identity_Windows.ps1
#	

function Get-CurrentScriptDirectory()
{
	return Split-Path $script:MyInvocation.MyCommand.Path
}

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

function Get-InstallationSettings($configSection)
{
	$installationConfig = [xml](Get-Content install.config)
	$sectionSettings = $installationConfig.installation.settings.scope | where {$_.name -eq $configSection}
	$installationSettings = @{}

	foreach($variable in $sectionSettings.variable){
		if($variable.name -and $variable.value){
			$installationSettings.Add($variable.name, $variable.value)
		}
	}

	$commonSettings = $installationConfig.installation.settings.scope | where {$_.name -eq "common"}
	foreach($variable in $commonSettings.variable){
		if($variable.name -and $variable.value -and !$installationSettings.Contains($variable.name)){
			$installationSettings.Add($variable.name, $variable.value)
		}
	}
	
	$encryptionCertificate = Get-EncryptionCertificate $installationSettings.encryptionCertificateThumbprint
	$installationSettingsDecrypted = @{}
	foreach($key in $installationSettings.Keys){
		$value = $installationSettings[$key]
		if($value.StartsWith("!!enc!!:"))
		{
			$value = Get-DecryptedString $encryptionCertificate $value
		}
		$installationSettingsDecrypted.Add($key, $value)
	}
	
	return $installationSettingsDecrypted
}

function Create-InstallationSetting($configSection, $configSetting, $configValue)
{
	$currentDirectory = Get-CurrentScriptDirectory
	$configFile = "install.config"
	$installationConfig = [xml](Get-Content "$currentDirectory\$configFile")
	$sectionSettings = $installationConfig.installation.settings.scope | where {$_.name -eq $configSection}
	$existingSetting = $sectionSettings.variable | where {$_.name -eq $configSetting}
	if($existingSetting -eq $null){
		$setting = $installationConfig.CreateElement("variable")
		
		$nameAttribute = $installationConfig.CreateAttribute("name")
		$nameAttribute.Value = $configSetting
		$setting.Attributes.Append($nameAttribute)

		$valueAttribute = $installationConfig.CreateAttribute("value")
		$valueAttribute.Value = $configValue
		$setting.Attributes.Append($valueAttribute)

		$sectionSettings.AppendChild($setting)
	}else{
		$existingSetting.value = $configValue
	}
	$installationConfig.Save("$currentDirectory\$configFile")
}

function Create-SecureInstallationSetting($configSection, $configSetting, $configValue, $encryptionCertificate)
{
	$encryptedConfigValue = Get-EncryptedString $encryptionCertificate $configValue
	Create-InstallationSetting $configSection $configSetting $encryptedConfigValue
}

function Get-EncryptionCertificate($encryptionCertificateThumbprint)
{
	return Get-Item Cert:\LocalMachine\My\$encryptionCertificateThumbprint
}

function Get-DecryptedString($encryptionCertificate, $encryptedString){
	if($encryptedString.Contains("!!enc!!:")){
		$cleanedEncryptedString = $encryptedString.Replace("!!enc!!:","")
		$clearTextValue = [System.Text.Encoding]::UTF8.GetString($encryptionCertificate.PrivateKey.Decrypt([System.Convert]::FromBase64String($cleanedEncryptedString), $true))
		return $clearTextValue
	}else{
		return $encryptedString
	}
}

$installSettings = Get-InstallationSettings "identity"
$zipPackage = $installSettings.zipPackage
$webroot = $installSettings.webroot
$appName = $installSettings.appName
$iisUser = $installSettings.iisUser
$primarySigningCertificateThumbprint = $installSettings.primarySigningCertificateThumbprint
$couchDbServer = $installSettings.couchDbServer
$couchDbUsername = $installSettings.couchDbUsername
$couchDbPassword = $installSettings.couchDbPassword 
$appInsightsInstrumentationKey = $installSettings.appInsightsInstrumentationKey
$siteName = $installSettings.siteName
$hostUrl = $installSettings.hostUrl

$workingDirectory = Get-CurrentScriptDirectory
if(!(Test-Path $workingDirectory\Fabric-Install-Utilities.psm1)){
	Invoke-WebRequest -Uri https://raw.githubusercontent.com/HealthCatalyst/InstallScripts/master/common/Fabric-Install-Utilities.psm1 -OutFile Fabric-Install-Utilities.psm1
}
Import-Module -Name .\Fabric-Install-Utilities.psm1

if(!(Test-Prerequisite '*.NET Core*Windows Server Hosting*' 1.1.30327.81))
{
    Write-Host ".NET Core Windows Server Hosting Bundle minimum version 1.1.30327.81 not installed...download and install from https://go.microsoft.com/fwlink/?linkid=844461. Halting installation."
    exit 1
}else{
    Write-Host ".NET Core Windows Server Hosting Bundle installed and meets expectations."
}

if(!(Test-Prerequisite '*CouchDB*'))
{
    Write-Host "CouchDB not installed locally, testing to see if is installed on a remote server using $couchDbServer"
    $remoteInstallationStatus = Get-CouchDbRemoteInstallationStatus $couchDbServer 2.0.0
    if($remoteInstallationStatus -eq "NotInstalled")
    {
        Write-Host "CouchDB not installed, download and install from https://dl.bintray.com/apache/couchdb/win/2.1.0/apache-couchdb-2.1.0.msi. Halting installation."
		exit 1
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
Publish-WebSite $zipPackage $appDirectory $appName


#Write environment variables
Write-Host "Loading up environment variables..."
$environmentVariables = @{"HostingOptions__UseInMemoryStores" = "false"; "HostingOptions__UseTestUsers" = "false"; "AllowLocalLogin" = "false"}
$signingCert = Get-Item Cert:\LocalMachine\My\$primarySigningCertificateThumbprint

if($clientName){
	$environmentVariables.Add("ClientName", $clientName)
}

if ($primarySigningCertificateThumbprint){
	$environmentVariables.Add("SigningCertificateSettings__UseTemporarySigningCredential", "false")
	$environmentVariables.Add("SigningCertificateSettings__PrimaryCertificateThumbprint", $primarySigningCertificateThumbprint)
}

if ($couchDbServer){
	$environmentVariables.Add("CouchDbSettings__Server", $couchDbServer)
}

if ($couchDbUsername){
	$environmentVariables.Add("CouchDbSettings__Username", $couchDbUsername)
}

if ($couchDbPassword){
	$encryptedCouchDbPassword = Get-EncryptedString $signingCert $couchDbPassword
	$environmentVariables.Add("CouchDbSettings__Password", $encryptedCouchDbPassword)
}

if($appInsightsInstrumentationKey){
	$environmentVariables.Add("ApplicationInsights__Enabled", "true")
	$environmentVariables.Add("ApplicationInsights__InstrumentationKey", $appInsightsInstrumentationKey)
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
    "scopes":[{"name":"fabric/identity.manageresources"}]
}
'@

Write-Host "Registering Fabric.Identity registration api."
$registrationApiSecret = Add-ApiRegistration -authUrl $identityServerUrl -body $body

#Register Fabric.Installer
$body = @'
{
    "clientId":"fabric-installer", 
    "clientName":"Fabric Installer", 
    "requireConsent":"false", 
    "allowedGrantTypes": ["client_credentials"], 
    "allowedScopes": ["fabric/identity.manageresources", "fabric/authorization.read", "fabric/authorization.write", "fabric/authorization.manageclients"]
}
'@

Write-Host "Registering Fabric.Installer."
$installerClientSecret = Add-ClientRegistration -authUrl $identityServerUrl -body $body
Create-SecureInstallationSetting "common" "fabricInstallerSecret" $installerClientSecret $signingCert

Write-Host ""
Write-Host "Please keep the following secrets in a secure place:"
Write-Host "Fabric.Installer clientSecret: $installerClientSecret"
Write-Host "Fabric.Registration apiSecret: $registrationApiSecret"
Write-Host ""
Write-Host "The Fabric.Installer clientSecret will be needed in subsequent installations:"
Write-Host "Fabric.Installer clientSecret: $installerClientSecret"

Write-Host "Installation complete, exiting."