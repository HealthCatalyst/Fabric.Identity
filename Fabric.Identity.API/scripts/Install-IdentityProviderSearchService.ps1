param(
    [PSCredential] $credential, 
    [Hashtable] $configStore = @{Type = "File"; Format = "XML"; Path = "$PSScriptRoot\install.config"},
    [switch] $noDiscoveryService, 
    [switch] $quiet
)

Import-Module -Name .\Install-Identity-Utilities.psm1 -Force

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
Set-LoggingConfiguration -commonConfig $commonConfigStore
$sqlServerAddress = Get-SqlServerAddress -sqlServerAddress $commonConfigStore.sqlServerAddress -installConfigPath $configStore.Path -quiet $quiet
$metadataDatabase = Get-MetadataDatabaseConnectionString -metadataDbName $commonConfigStore.metadataDbName -sqlServerAddress $sqlServerAddress -installConfigPath $configStore.Path -quiet $quiet
$appInsightsKey = Get-AppInsightsKey -appInsightsInstrumentationKey $identityConfigStore.appInsightsInstrumentationKey -installConfigPath $configStore.Path -scope $identitySettingsScope -quiet $quiet

# Pre-check requirements to run this script
if([string]::IsNullOrEmpty($commonConfigStore.webServerDomain) -or [string]::IsNullOrEmpty($commonConfigStore.clientEnvironment))
{
  Write-DosMessage -Level "Error" -Message "It is required to have 'webServerDomain' and 'clientEnvironment' populated in install.config."
}

# Prompt for the credential if it is null
if($null -eq $credential)
{
  $idpssIisUser = Get-IISAppPoolUser -credential $credential -appName $idpssConfigStore.appName -storedIisUser $idpssConfigStore.iisUser -installConfigPath $configStore.Path -scope $idpssSettingsScope
  $credential = $idpssIisUser.Credential
}

$idpssServiceUrl = Get-ApplicationEndpoint -appName $idpssConfigStore.appName -applicationEndpoint $idpssConfigStore.applicationEndPoint -installConfigPath $configStore.Path -scope $idpssSettingsScope -quiet $quiet
    
$idpssStandalonePath = ".\Fabric.IdentityProviderSearchService.zip"
$idpssInstallerPath = "..\WebDeployPackages\Fabric.IdentityProviderSearchService.zip"
$idpssInstallPackagePath = Get-WebDeployPackagePath -standalonePath $idpssStandalonePath -installerPath $idpssInstallerPath

$secretNoEnc = $commonConfigStore.fabricInstallerSecret -replace "!!enc!!:"

$decryptedSecret = Unprotect-DosInstallerSecret -CertificateThumprint $commonConfigStore.encryptionCertificateThumbprint -EncryptedInstallerSecretValue $secretNoEnc

$registrationApiSecret = Add-IdpssApiResourceRegistration -identityServiceUrl $commonConfigStore.identityService -fabricInstallerSecret $decryptedSecret

$idpssWebDeployParameters = Get-IdpssWebDeployParameters -serviceConfig $idpssConfigStore `
                        -commonConfig $commonConfigStore `
                        -discoveryServiceUrl $discoveryServiceUrl `
                        -noDiscoveryService $noDiscoveryService `
                        -credential $credential `
                        -registrationApiSecret $registrationApiSecret `
                        -metadataConnectionString $metadataDatabase.DbConnectionString

$idpssInstallApplication = Publish-DosWebApplication -WebAppPackagePath $idpssInstallPackagePath `
                      -WebDeployParameters $idpssWebDeployParameters `
                      -AppPoolName $idpssConfigStore.appPoolName `
                      -AppPoolCredential $credential `
                      -AuthenticationType "Anonymous" `
                      -WebDeploy

$idpssAppPoolUser = $credential.UserName

$idpssName = "IdentityProviderSearchService"

$certificates = Get-Certificates -primarySigningCertificateThumbprint $identityConfigStore.primarySigningCertificateThumbprint `
            -encryptionCertificateThumbprint $identityConfigStore.encryptionCertificateThumbprint `
            -installConfigPath $configStore.Path `
            -scope $identitySettingsScope `
            -quiet $quiet

Add-PermissionToPrivateKey $idpssAppPoolUser $certificates.SigningCertificate read

$idpssDirectory = [io.path]::combine([System.Environment]::ExpandEnvironmentVariables($selectedSite.physicalPath), $idpssName)
New-LogsDirectoryForApp $idpssDirectory $idpssAppPoolUser

Register-ServiceWithDiscovery -iisUserName $idpssAppPoolUser -metadataConnStr $metadataDatabase.DbConnectionString -version $idpssInstallApplication.version -serverUrl $idpssServiceUrl `
-serviceName $idpssName -friendlyName "Fabric.IdentityProviderSearchService" -description "The Fabric.IdentityProviderSearchService searches Identity Providers for matching users and groups.";

$idpssConfig = $idpssDirectory + "\web.config"

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

Set-IdentityProviderSearchServiceWebConfigSettings -webConfigPath $idpssConfig `
    -useAzure $useAzure `
    -useWindows $useWindows `
    -installConfigPath $configStore.Path `
    -encryptionCert $certificates.SigningCertificate `
    -encryptionCertificateThumbprint $certificates.EncryptionCertificate.Thumbprint `
    -appInsightsInstrumentationKey $appInsightsKey `
    -appName $idpssName 