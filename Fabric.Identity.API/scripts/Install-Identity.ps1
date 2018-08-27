param([switch]$noDiscoveryService, [switch] $quiet)
Import-Module -Name .\Install-Identity-Utilities.psm1 -Force

# Import Fabric Install Utilities
$fabricInstallUtilities = ".\Fabric-Install-Utilities.psm1"
if (!(Test-Path $fabricInstallUtilities -PathType Leaf)) {
    Write-DosMessage -Level "Warning" -Message "Could not find fabric install utilities. Manually downloading and installing"
    Invoke-WebRequest -Uri https://raw.githubusercontent.com/HealthCatalyst/InstallScripts/master/common/Fabric-Install-Utilities.psm1 -Headers @{"Cache-Control" = "no-cache"} -OutFile $fabricInstallUtilities
}
Import-Module -Name $fabricInstallUtilities -Force

if(!(Test-IsRunAsAdministrator))
{
    Write-DosMessage -Level "Error" -Message "You must run this script as an administrator. Halting configuration."
    throw
}

$installSettings = Get-InstallationSettings "identity"
$zipPackage = Get-FullyQualifiedInstallationZipFile -zipPackage $installSettings.zipPackage -workingDirecory $PSScriptRoot
Install-DotNetCoreIfNeeded -version "1.1.30503.82" -downloadUrl "https://go.microsoft.com/fwlink/?linkid=848766"
$selectedSite = Get-IISWebSiteForInstall -selectedSiteName $installSettings.siteName -quiet $quiet
$selectedCerts = Get-Certificates -primarySigningCertificateThumbprint $installSettings.primarySigningCertificateThumbprint -encryptionCertificateThumbprint $installSettings.encryptionCertificateThumbprint -quiet $quiet
$iisUser = Get-IISAppPoolUser -appName $installSettings.appName -storedIisUser $installSettings.iisUser
Add-PermissionToPrivateKey $iisUser.UserName $selectedCerts.SigningCertificate read
$appInsightsKey = Get-AppInsightsKey -appInsightsInstrumentationKey $installSettings.appInsightsInstrumentationKey -quiet $quiet
$sqlServerAddress = Get-SqlServerAddress -sqlServerAddress $installSettings.sqlServerAddress -quiet $quiet
$identityDatabase = Get-IdentityDatabaseConnectionString -identityDbName $installSettings.identityDbName -sqlServerAddress $sqlServerAddress -quiet $quiet
if(!$noDiscoveryService){
    $metadataDatabase = Get-MetadataDatabaseConnectionString -metadataDbName $installSettings.metadataDbName -sqlServerAddress $sqlServerAddress -quiet $quiet
    $discoveryServiceUrl = Get-DiscoveryServiceUrl -discoveryServiceUrl $installSettings.discoveryService -quiet $quiet
}
$identityServiceUrl = Get-ApplicationEndpoint -appName $installSettings.appName -applicationEndpoint $installSettings.applicationEndPoint -quiet $quiet

Unlock-ConfigurationSections
$installApplication = Publish-Identity -site $selectedSite `
                 -appName $installSettings.appName `
                 -iisUser $iisUser `
                 -zipPackage $zipPackage `

Add-DatabaseSecurity $iisUser.UserName $installSettings.identityDatabaseRole $identityDatabase.DbConnectionString
if(!$noDiscoveryService){
    Register-IdentityWithDiscovery -iisUserName $iisUser.UserName -metadataConnStr $metadataDatabase.DbConnectionString -version $installApplication.version -identityServerUrl $identityServiceUrl
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

if(Test-RegistrationComplete $identityServerUrl) {
    $accessToken = Get-AccessToken -authUrl $identityServerUrl -clientId "fabric-installer" -scope "fabric/identity.manageresources" -secret $installSettings.fabricInstallerSecret
}

$registrationApiSecret = Add-RegistrationApiRegistration -identityServerUrl $identityServiceUrl -accessToken $accessToken
$fabricInstallerSecret = Add-InstallerClientRegistration -identityServerUrl $identityServiceUrl -accessToken $accessToken -fabricInstallerSecret $installSettings.fabricInstallerSecret
Add-SecureInstallationSetting "common" "fabricInstallerSecret" $fabricInstallerSecret $selectedCerts.SigningCertificate

if (!$accessToken){
    $accessToken = Get-AccessToken -authUrl $identityServiceUrl -clientId "fabric-installer" -scope "fabric/identity.manageresources" -secret $fabricInstallerSecret
}

$identityClientSecret = Add-IdentityClientRegistration -identityServerUrl $identityServiceUrl -accessToken $accessToken
Add-SecureIdentityEnvironmentVariables -encryptionCert $selectedCerts.SigningCertificate `
    -identityClientSecret $identityClientSecret `
    -registrationApiSecret $registrationApiSecret `
    -appDirectory $installApplication.applicationDirectory

if ($fabricInstallerSecret){
    Write-DosMessage -Level "Information" -Message "Please keep the following Fabric.Installer secret in a secure place, it will be needed in subsequent installations:"
    Write-DosMessage -Level "Information" -Message "Fabric.Installer clientSecret: $fabricInstallerSecret"
}

Read-Host -Prompt "Installation complete, press Enter to exit"