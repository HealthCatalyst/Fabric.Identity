param(
    [PSCredential] $credential, 
    [Hashtable] $configStore = @{Type = "File"; Format = "XML"; Path = "$PSScriptRoot\install.config"},
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

# Especially calling this script from another script, this message is helpful
Write-DosMessage -Level "Information" -Message "Starting IdentityProviderSearchService installation..."

# Get Idpss app pool user 
# Create log directory with read/write permissions for app pool user
# using methods in DosInstallUtilites to install idpss, which will make it easier to migrate the identity code later 
$idpssSettingsScope = "identityProviderSearchService"
$idpssConfigStore = Get-DosConfigValues -ConfigStore $configStore -Scope $idpssSettingsScope
$commonConfigStore = Get-DosConfigValues -ConfigStore $configStore -Scope "common"
$identitySettingsScope = "identity"
$identityConfigStore = Get-DosConfigValues -ConfigStore $configStore -Scope $identitySettingsScope
$idpssInstallSettings = Get-InstallationSettings $idpssSettingsScope -installConfigPath $configStore.Path
Set-LoggingConfiguration -commonConfig $commonConfigStore

$certificates = Get-Certificates -primarySigningCertificateThumbprint $identityConfigStore.primarySigningCertificateThumbprint `
            -encryptionCertificateThumbprint $identityConfigStore.encryptionCertificateThumbprint `
            -installConfigPath $configStore.Path `
            -scope $identitySettingsScope `
            -quiet $quiet
$idpssIisUser = Get-IISAppPoolUser -credential $credential -appName $idpssConfigStore.appName -storedIisUser $idpssConfigStore.iisUser -installConfigPath $configStore.Path -scope $idpssSettingsScope
Add-PermissionToPrivateKey $idpssIisUser.UserName $certificates.SigningCertificate read
$appInsightsKey = Get-AppInsightsKey -appInsightsInstrumentationKey $identityConfigStore.appInsightsInstrumentationKey -installConfigPath $configStore.Path -scope $identitySettingsScope -quiet $quiet
$sqlServerAddress = Get-SqlServerAddress -sqlServerAddress $commonConfigStore.sqlServerAddress -installConfigPath $configStore.Path -quiet $quiet
$metadataDatabase = Get-MetadataDatabaseConnectionString -metadataDbName $commonConfigStore.metadataDbName -sqlServerAddress $sqlServerAddress -installConfigPath $configStore.Path -quiet $quiet

if(!$noDiscoveryService){
    $discoveryServiceUrl = Get-DiscoveryServiceUrl -discoveryServiceUrl $commonConfigStore.discoveryService -installConfigPath $configStore.Path -quiet $quiet
}

$idpssServiceUrl = Get-ApplicationEndpoint -appName $idpssConfigStore.appName -applicationEndpoint $idpssConfigStore.applicationEndPoint -installConfigPath $configStore.Path -scope $idpssSettingsScope -quiet $quiet
$currentUserDomain = Get-CurrentUserDomain -quiet $quiet
    
$idpssStandalonePath = "$PSScriptRoot\Fabric.IdentityProviderSearchService.zip"
$idpssInstallerPath = "$PSScriptRoot\..\WebDeployPackages\Fabric.IdentityProviderSearchService.zip"
$idpssInstallPackagePath = Get-WebDeployPackagePath -standalonePath $idpssStandalonePath -installerPath $idpssInstallerPath
$selectedSite = Get-IISWebSiteForInstall -selectedSiteName $idpssInstallSettings.siteName -quiet $quiet -installConfigPath $configStore.Path -scope $idpssSettingsScope

$secretNoEnc = $commonConfigStore.fabricInstallerSecret -replace "!!enc!!:"

$decryptedSecret = Unprotect-DosInstallerSecret -CertificateThumprint $commonConfigStore.encryptionCertificateThumbprint -EncryptedInstallerSecretValue $secretNoEnc

$registrationApiSecret = Add-IdpssApiResourceRegistration -identityServiceUrl $commonConfigStore.identityService -fabricInstallerSecret $decryptedSecret

$idpssWebDeployParameters = Get-IdpssWebDeployParameters -serviceConfig $idpssConfigStore `
                        -commonConfig $commonConfigStore `
                        -applicationEndpoint $idpssServiceUrl `
                        -discoveryServiceUrl $discoveryServiceUrl `
                        -noDiscoveryService $noDiscoveryService `
                        -registrationApiSecret $registrationApiSecret `
                        -metadataConnectionString $metadataDatabase.DbConnectionString `
                        -currentDomain $currentUserDomain

$idpssInstallApplication = Publish-DosWebApplication -WebAppPackagePath $idpssInstallPackagePath `
                      -WebDeployParameters $idpssWebDeployParameters `
                      -AppPoolName $idpssConfigStore.appPoolName `
                      -AppName $idpssConfigStore.appName `
                      -AppPoolCredential $idpssIisUser.Credential `
                      -AuthenticationType "Anonymous" `
                      -WebDeploy

$idpssDirectory = [io.path]::combine([System.Environment]::ExpandEnvironmentVariables($selectedSite.physicalPath), $idpssConfigStore.appName)
Write-Host "IdPSS Directory: $($idpssDirectory)"
New-LogsDirectoryForApp $idpssDirectory $idpssIisUser.UserName

Register-ServiceWithDiscovery -iisUserName $idpssIisUser.UserName -metadataConnStr $metadataDatabase.DbConnectionString -version $idpssInstallApplication.version -serverUrl $idpssServiceUrl `
-serviceName $idpssConfigStore.appName -friendlyName "Fabric.IdentityProviderSearchService" -description "The Fabric.IdentityProviderSearchService searches Identity Providers for matching users and groups.";

$idpssConfig = $idpssDirectory + "\web.config"
Write-Host "IdPSS Web Config: $($idpssConfig)"

$useAzure = $identityConfigStore.useAzureAD
if($null -eq $useAzure) {
    $useAzure = $false
    Add-InstallationSetting -configSection $identitySettingsScope -configSetting "useAzureAD" -configValue "$useAzure" -installConfigPath $configStore.Path  | Out-Null
}

$useWindows = $identityConfigStore.useWindowsAD
if($null -eq $useWindows) {
    $useWindows = $true
    Add-InstallationSetting -configSection $identitySettingsScope -configSetting "useWindowsAD" -configValue "$useWindows" -installConfigPath $configStore.Path  | Out-Null
}

$idpssName = $Global:idPSSAppName

Set-IdentityProviderSearchServiceWebConfigSettings -webConfigPath $idpssConfig `
    -useAzure $useAzure `
    -useWindows $useWindows `
    -installConfigPath $configStore.Path `
    -encryptionCert $certificates.SigningCertificate `
    -encryptionCertificateThumbprint $certificates.EncryptionCertificate.Thumbprint `
    -appInsightsInstrumentationKey $appInsightsKey `
    -appName $idpssName 