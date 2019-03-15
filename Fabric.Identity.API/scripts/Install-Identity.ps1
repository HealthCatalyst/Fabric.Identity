#Requires -RunAsAdministrator
#Requires -Version 5.1
#Requires -Modules PowerShellGet, PackageManagement

param(
    [PSCredential] $credential,
    [ValidateScript({
        if (!(Test-Path $_)) {
            throw "Path $_ does not exist. Please enter valid path to the install.config."
        }
        if (!(Test-Path $_ -PathType Leaf)) {
            throw "Path $_ is not a file. Please enter a valid path to the install.config."
        }
        return $true
    })] 
    [string] $installConfigPath = "$PSScriptRoot\install.config", 
    [switch] $noDiscoveryService, 
    [switch] $quiet
)
Import-Module -Name .\Install-Identity-Utilities.psm1 -Force

# Import Fabric Install Utilities
$fabricInstallUtilities = ".\Fabric-Install-Utilities.psm1"
if (!(Test-Path $fabricInstallUtilities -PathType Leaf)) {
    Write-DosMessage -Level "Warning" -Message "Could not find fabric install utilities. Manually downloading and installing"
    Get-WebRequestDownload -Uri https://raw.githubusercontent.com/HealthCatalyst/InstallScripts/master/common/Fabric-Install-Utilities.psm1 -NoCache -OutFile $fabricInstallUtilities
}
Import-Module -Name $fabricInstallUtilities -Force

Test-MeetsMinimumRequiredPowerShellVerion -majorVersion 5

if(!(Test-IsRunAsAdministrator))
{
    Write-DosMessage -Level "Error" -Message "You must run this script as an administrator. Halting configuration."
    throw
}

$ErrorActionPreference = "Stop"

Write-DosMessage -Level "Information" -Message "Using install.config: $installConfigPath"
$installSettingsScope = "identity"
$installSettings = Get-InstallationSettings $installSettingsScope -installConfigPath $installConfigPath

$commonSettingsScope = "common"
$commonInstallSettings = Get-InstallationSettings $commonSettingsScope -installConfigPath $installConfigPath
Set-LoggingConfiguration -commonConfig $commonInstallSettings

$currentDirectory = $PSScriptRoot
$zipPackage = Get-FullyQualifiedInstallationZipFile -zipPackage $installSettings.zipPackage -workingDirectory $currentDirectory
Install-DotNetCoreIfNeeded -version "1.1.30503.82" -downloadUrl "https://go.microsoft.com/fwlink/?linkid=848766"
$selectedSite = Get-IISWebSiteForInstall -selectedSiteName $installSettings.siteName -quiet $quiet -installConfigPath $installConfigPath -scope $installSettingsScope
$selectedCerts = Get-Certificates -primarySigningCertificateThumbprint $installSettings.primarySigningCertificateThumbprint -encryptionCertificateThumbprint $installSettings.encryptionCertificateThumbprint -installConfigPath $installConfigPath -scope $installSettingsScope -quiet $quiet
$iisUser = Get-IISAppPoolUser -credential $credential -appName $installSettings.appName -storedIisUser $installSettings.iisUser -installConfigPath $installConfigPath -scope $installSettingsScope
Add-PermissionToPrivateKey $iisUser.UserName $selectedCerts.SigningCertificate read
$appInsightsKey = Get-AppInsightsKey -appInsightsInstrumentationKey $installSettings.appInsightsInstrumentationKey -installConfigPath $installConfigPath -scope $installSettingsScope -quiet $quiet
$sqlServerAddress = Get-SqlServerAddress -sqlServerAddress $installSettings.sqlServerAddress -installConfigPath $installConfigPath -quiet $quiet
$identityDatabase = Get-IdentityDatabaseConnectionString -identityDbName $installSettings.identityDbName -sqlServerAddress $sqlServerAddress -installConfigPath $installConfigPath -quiet $quiet
$metadataDatabase = Get-MetadataDatabaseConnectionString -metadataDbName $installSettings.metadataDbName -sqlServerAddress $sqlServerAddress -installConfigPath $installConfigPath -quiet $quiet

if(!$noDiscoveryService){
    $discoveryServiceUrl = Get-DiscoveryServiceUrl -discoveryServiceUrl $installSettings.discoveryService -installConfigPath $installConfigPath -quiet $quiet
}
$identityServiceUrl = Get-ApplicationEndpoint -appName $installSettings.appName -applicationEndpoint $installSettings.applicationEndPoint -installConfigPath $installConfigPath -scope $installSettingsScope -quiet $quiet

Unlock-ConfigurationSections
$installApplication = Publish-Application -site $selectedSite `
                 -appName $installSettings.appName `
                 -iisUser $iisUser `
                 -zipPackage $zipPackage `
                 -assembly "Fabric.Identity.API.dll"

Add-DatabaseSecurity $iisUser.UserName $installSettings.identityDatabaseRole $identityDatabase.DbConnectionString
if(!$noDiscoveryService){
    Register-ServiceWithDiscovery -iisUserName $iisUser.UserName -metadataConnStr $metadataDatabase.DbConnectionString -version $installApplication.version -serverUrl $identityServiceUrl `
    -serviceName "IdentityService" -friendlyName "Fabric.Identity" -description "The Fabric.Identity service provides centralized authentication across the Fabric ecosystem.";
}

Set-IdentityEnvironmentVariables -appDirectory $installApplication.applicationDirectory `
-primarySigningCertificateThumbprint $selectedCerts.SigningCertificate.Thumbprint `
-encryptionCertificateThumbprint $selectedCerts.EncryptionCertificate.Thumbprint `
-appInsightsInstrumentationKey $appInsightsKey `
-applicationEndpoint $identityServiceUrl `
-identityDbConnStr $identityDatabase.DbConnectionString`
-discoveryServiceUrl $discoveryServiceUrl `
-noDiscoveryService $noDiscoveryService

$accessToken = ""

if(Test-RegistrationComplete $identityServiceUrl) {
    $accessToken = Get-AccessToken -authUrl $identityServiceUrl -clientId "fabric-installer" -scope "fabric/identity.manageresources" -secret $installSettings.fabricInstallerSecret
}

$registrationApiSecret = Add-RegistrationApiRegistration -identityServerUrl $identityServiceUrl -accessToken $accessToken
$fabricInstallerSecret = Add-InstallerClientRegistration -identityServerUrl $identityServiceUrl -accessToken $accessToken -fabricInstallerSecret $installSettings.fabricInstallerSecret
Add-SecureInstallationSetting "common" "fabricInstallerSecret" $fabricInstallerSecret $selectedCerts.SigningCertificate $installConfigPath

if (!$accessToken){
    $accessToken = Get-AccessToken -authUrl $identityServiceUrl -clientId "fabric-installer" -scope "fabric/identity.manageresources" -secret $fabricInstallerSecret
}

$identityClientSecret = Add-IdentityClientRegistration -identityServerUrl $identityServiceUrl -accessToken $accessToken
Add-SecureIdentityEnvironmentVariables -encryptionCert $selectedCerts.SigningCertificate `
    -identityClientSecret $identityClientSecret `
    -registrationApiSecret $registrationApiSecret `
    -appDirectory $installApplication.applicationDirectory

$useAzure = $installSettings.useAzureAD
if($null -eq $useAzure) {
    $useAzure = $false
    Add-InstallationSetting -configSection $installSettingsScope -configSetting "useAzureAD" -configValue "$useAzure" -installConfigPath $installConfigPath | Out-Null
}

$useWindows = $installSettings.useWindowsAD
if($null -eq $useWindows) {
    $useWindows = $true
    Add-InstallationSetting -configSection $installSettingsScope -configSetting "useWindowsAD" -configValue "$useWindows" -installConfigPath $installConfigPath | Out-Null
}

Set-IdentityEnvironmentAzureVariables -appConfig $installApplication.applicationDirectory `
    -useAzure $useAzure `
    -useWindows $useWindows `
    -installConfigPath $installConfigPath `
    -encryptionCert $selectedCerts.SigningCertificate

Set-IdentityUri -identityUri $identityServiceUrl `
    -connString $metadataDatabase.DbConnectionString

if ($fabricInstallerSecret){
    Write-DosMessage -Level "Information" -Message "Fabric.Installer clientSecret has been created."
}

# Call the Idpss powershell script
.\Install-IdentityProviderSearchService.ps1 -certificates $($selectedCerts)


if(!$quiet){
    Read-Host -Prompt "Installation complete, press Enter to exit"
}

