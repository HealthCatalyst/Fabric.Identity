param(
    [Parameter(Mandatory = $True)]
    [PSCredential] $credential, 
    [Hashtable] $configStore = @{Type = "File"; Format = "XML"; Path = "$PSScriptRoot\install.config"},
    [Hashtable] $certificates,
    [switch] $quiet
)

Import-Module -Name .\Install-Identity-Utilities.psm1 -Force

# Get Idpss app pool user 
# Create log directory with read/write permissions for app pool user
# using methods in DosInstallUtilites to install idpss, which will make it easier to migrate the identity code later 
$idpssSettingsScope = "identityProviderSearchService"
$idpssConfigStore = Get-DosConfigValues -ConfigStore $configStore -Scope $idpssSettingsScope
$commonConfigStore = Get-DosConfigValues -ConfigStore $configStore -Scope "common"

$idpssServiceUrl = Get-ApplicationEndpoint -appName $idpssConfigStore.appName -applicationEndpoint $idpssConfigStore.applicationEndPoint -installConfigPath $installConfigPath -scope $idpssSettingsScope -quiet $quiet
    
$idpssStandalonePath = ".\Fabric.IdentityProviderSearchService.zip"
$idpssInstallerPath = "..\WebDeployPackages\Fabric.IdentityProviderSearchService.zip"
$idpssInstallPackagePath = Get-WebDeployPackagePath -standalonePath $idpssStandalonePath -installerPath $idpssInstallerPath

$secretNoEnc = $commonConfigStore.fabricInstallerSecret -replace "!!enc!!:"

$decryptedSecret = Unprotect-DosInstallerSecret -CertificateThumprint $commonConfigStore.encryptionCertificateThumbprint -EncryptedInstallerSecretValue $secretNoEnc

$registrationApiSecret = Add-IdpssApiResourceRegistration -identityServiceUrl $commonConfigStore.identityService -fabricInstallerSecret $decryptedSecret

$idpssWebDeployParameters = Get-IdpssWebDeployParameters -serviceConfig $idpssConfigStore -commonConfig $commonConfigStore -discoveryServiceUrl $discoveryServiceUrl -noDiscoveryService $noDiscoveryService -registrationApiSecret $registrationApiSecret -credential $credential

$idpssInstallApplication = Publish-DosWebApplication -WebAppPackagePath $idpssInstallPackagePath `
                      -WebDeployParameters $idpssWebDeployParameters `
                      -AppPoolName $idpssConfigStore.appPoolName `
                      -AppPoolCredential $credential `
                      -AuthenticationType "Anonymous" `
                      -WebDeploy `

$idpssAppPoolUser = $credential.UserName

$idpssName = "IdentityProviderSearchService"

Add-PermissionToPrivateKey $idpssAppPoolUser $certificates.SigningCertificate read

$idpssDirectory = [io.path]::combine([System.Environment]::ExpandEnvironmentVariables($selectedSite.physicalPath), $idpssName)
New-AppRoot $idpssDirectory $idpssAppPoolUser

Register-ServiceWithDiscovery -iisUserName $idpssAppPoolUser -metadataConnStr $metadataDatabase.DbConnectionString -version $idpssInstallApplication.version -serverUrl $idpssServiceUrl `
-serviceName $idpssName -friendlyName "Fabric.IdentityProviderSearchService" -description "The Fabric.IdentityProviderSearchService searches Identity Providers for matching users and groups.";

$idpssConfig = $idpssDirectory + "\web.config"

Set-IdentityProviderSearchServiceWebConfigSettings -webConfigPath $idpssConfig `
    -useAzure $useAzure `
    -useWindows $useWindows `
    -installConfigPath $installConfigPath `
    -encryptionCert $certificates.SigningCertificate `
    -encryptionCertificateThumbprint $certificates.EncryptionCertificate.Thumbprint `
    -appInsightsInstrumentationKey $appInsightsKey `
    -appName $idpssName 