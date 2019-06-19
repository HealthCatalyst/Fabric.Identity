#Requires -RunAsAdministrator
#Requires -Version 5.1
#Requires -Modules PowerShellGet, PackageManagement

param(
    [PSCredential] $credential,
    [Hashtable] $configStore = @{Type = "File"; Format = "XML"; Path = "$PSScriptRoot\install.config"},
    [switch] $noDiscoveryService,
    [switch] $quiet
)
if (!(Test-Path $configStore.Path)) {
    throw "Path $($configStore.Path) does not exist. Please enter valid path to the install.config."
}
if (!(Test-Path $configStore.Path -PathType Leaf)) {
    throw "Path $($configStore.Path) is not a file. Please enter a valid path to the install.config."
}
Import-Module -Name .\Install-Identity-Utilities.psm1 -Force

# Import Fabric Install Utilities
$fabricInstallUtilities = "$PSScriptRoot\Fabric-Install-Utilities.psm1"
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

# Read in Configuration settings from install.config
$ErrorActionPreference = "Stop"
Write-DosMessage -Level "Information" -Message "Using install.config: $($configStore.Path)"
$installSettingsScope = "identity"
$installSettings = Get-InstallationSettings $installSettingsScope -installConfigPath $configStore.Path

$commonSettingsScope = "common"
$commonInstallSettings = Get-InstallationSettings $commonSettingsScope -installConfigPath $configStore.Path
Set-LoggingConfiguration -commonConfig $commonInstallSettings


# Setup connection strings and dependences
$currentDirectory = $PSScriptRoot
$zipPackage = Get-FullyQualifiedInstallationZipFile -zipPackage $installSettings.zipPackage -workingDirectory $currentDirectory
Install-DotNetCoreIfNeeded -version "1.1.30503.82" -downloadUrl "https://go.microsoft.com/fwlink/?linkid=848766"
$selectedSite = Get-IISWebSiteForInstall -selectedSiteName $installSettings.siteName -quiet $quiet -installConfigPath $configStore.Path -scope $installSettingsScope
$selectedCerts = Get-Certificates -primarySigningCertificateThumbprint $installSettings.primarySigningCertificateThumbprint -encryptionCertificateThumbprint $installSettings.encryptionCertificateThumbprint -installConfigPath $configStore.Path -scope $installSettingsScope -quiet $quiet
$iisUser = Get-IISAppPoolUser -credential $credential -appName $installSettings.appName -storedIisUser $installSettings.iisUser -installConfigPath $configStore.Path -scope $installSettingsScope
Add-PermissionToPrivateKey $iisUser.UserName $selectedCerts.SigningCertificate read
$appInsightsKey = Get-AppInsightsKey -appInsightsInstrumentationKey $installSettings.appInsightsInstrumentationKey -installConfigPath $configStore.Path -scope $installSettingsScope -quiet $quiet
$sqlServerAddress = Get-SqlServerAddress -sqlServerAddress $installSettings.sqlServerAddress -installConfigPath $configStore.Path -quiet $quiet
$identityDatabase = Get-IdentityDatabaseConnectionString -identityDbName $installSettings.identityDbName -sqlServerAddress $sqlServerAddress -installConfigPath $configStore.Path -quiet $quiet
$metadataDatabase = Get-MetadataDatabaseConnectionString -metadataDbName $commonInstallSettings.metadataDbName -sqlServerAddress $sqlServerAddress -installConfigPath $configStore.Path -quiet $quiet

if(!$noDiscoveryService){
    $discoveryServiceUrl = Get-DiscoveryServiceUrl -discoveryServiceUrl $commonInstallSettings.discoveryService -installConfigPath $configStore.Path -quiet $quiet
}
$identityServiceUrl = Get-ApplicationEndpoint -appName $installSettings.appName -applicationEndpoint $installSettings.applicationEndPoint -installConfigPath $configStore.Path -scope $installSettingsScope -quiet $quiet

# back up filter settings
$appSettingsPath = "$($selectedSite.physicalPath)\$($installSettings.appName)\appsettings.json"
if (!(Test-Path $appSettingsPath)) {
    Write-DosMessage -Level "Information" -Message "Could not find $appSettingsPath to replace existing Fabric.Identity group claim filters."
    $appSettingsExists = $false
}
else {
    $appSettingsExists = $true
    $oldAppSettingsJson = Get-Content -Raw -Path "$appSettingsPath" | ConvertFrom-Json
    $oldFiltersJson = $oldAppSettingsJson.FilterSettings
}

Unlock-ConfigurationSections
$installApplication = Publish-Application -site $selectedSite `
                 -appName $installSettings.appName `
                 -iisUser $iisUser `
                 -zipPackage $zipPackage `
                 -assembly "Fabric.Identity.API.dll"

if ($appSettingsExists) {
    # restore filter settings
    Write-DosMessage -Level "Information" -Message "Copying existing Identity FilterSettings into $appSettingsPath."
    $newAppSettingsJson = Get-Content -Raw -Path "$appSettingsPath" | ConvertFrom-Json
    $newAppSettingsJson.FilterSettings = $oldFiltersJson
    $newAppSettingsJson | ConvertTo-Json -depth 100 | Out-File "$appSettingsPath"
}

# restore filter settings
$newAppSettingsJson = Get-Content -Raw -Path "$appSettingsPath" | ConvertFrom-Json
$newAppSettingsJson.FilterSettings = $oldFiltersJson
$newAppSettingsJson | ConvertTo-Json -depth 100 | Out-File "$appSettingsPath"

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
Add-SecureInstallationSetting "common" "fabricInstallerSecret" $fabricInstallerSecret $selectedCerts.SigningCertificate $configStore.Path

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
    Add-InstallationSetting -configSection $installSettingsScope -configSetting "useAzureAD" -configValue "$useAzure" -installConfigPath $configStore.Path | Out-Null
}

$useWindows = $installSettings.useWindowsAD
if($null -eq $useWindows) {
    $useWindows = $true
    Add-InstallationSetting -configSection $installSettingsScope -configSetting "useWindowsAD" -configValue "$useWindows" -installConfigPath $configStore.Path | Out-Null
}

Set-IdentityEnvironmentAzureVariables -appConfig $installApplication.applicationDirectory `
    -useAzure $useAzure `
    -useWindows $useWindows `
    -installConfigPath $configStore.Path `
    -encryptionCert $selectedCerts.SigningCertificate

Set-IdentityUri -identityUri $identityServiceUrl `
    -connString $metadataDatabase.DbConnectionString

if ($fabricInstallerSecret){
    Write-DosMessage -Level "Information" -Message "Fabric.Installer clientSecret has been created."
}
