param(
    [PSCredential] $credential, 
    [Hashtable] $configStore = @{Type = "File"; Format = "XML"; Path = "$PSScriptRoot\install.config"},
    [switch] $quiet
)
if (!(Test-Path $configStore.Path)) {
    throw "Path $($configStore.Path) does not exist. Please enter valid path to the install.config."
}
if (!(Test-Path $configStore.Path -PathType Leaf)) {
    throw "Path $($configStore.Path) is not a file. Please enter a valid path to the install.config."
}
Import-Module -Name .\Install-Identity-Utilities.psm1 -Force

Write-DosMessage -Level "Information" -Message "Starting DiscoveryService installation..."

$discoverySettingsScope = "discoveryservice"
$discoveryConfig = Get-DosConfigValues -ConfigStore $configStore -Scope $discoverySettingsScope
$commonConfig = Get-DosConfigValues -ConfigStore $configStore -Scope "common"
$identityConfig = Get-DosConfigValues -ConfigStore $configStore -Scope "identity"
Set-LoggingConfiguration -commonConfig $commonConfig

# Pre-check requirements to run this script
$webServerDomain = Get-WebServerDomain $commonConfig.webServerDomain $configStore.Path $quiet
if([string]::IsNullOrEmpty($webServerDomain))
{
  Write-DosMessage -Level "Error" -Message "It is required to have 'webServerDomain' populated in install.config." -ErrorAction Stop
}

if([string]::IsNullOrEmpty($commonConfig.clientEnvironment))
{
  Write-DosMessage -Level "Error" -Message "It is required to have 'clientEnvironment' populated in install.config." -ErrorAction Stop
}

$identityServiceUrl = $commonConfig.identityService
if([string]::IsNullOrEmpty($identityServiceUrl) -or !(Test-RegistrationComplete $identityServiceUrl))
{
    Write-DosMessage -Level "Error" -Message "IdentityService is not installed. With OAuth enabled, you need to install IdentityService first before proceeding." -ErrorAction Stop
}

$discoveryIisUser = Get-IISAppPoolUser -credential $credential -appName $discoveryConfig.appName -storedIisUser $discoveryConfig.iisUser -installConfigPath $configStore.Path -scope $discoverySettingsScope

$discoveryStandalonePath = "$PSScriptRoot\Catalyst.DiscoveryService.zip"
$discoveryInstallerPath = "$PSScriptRoot\..\WebDeployPackages\Catalyst.DiscoveryService.zip"
$installPackagePath = Get-WebDeployPackagePath -standalonePath $discoveryStandalonePath -installerPath $discoveryInstallerPath

$registrationApiSecret = $null
$authenticationType = "Windows", "Anonymous"

$secretNoEnc = $commonConfig.fabricInstallerSecret -replace "!!enc!!:"
$decryptedSecret = Unprotect-DosInstallerSecret -CertificateThumprint $commonConfig.encryptionCertificateThumbprint -EncryptedInstallerSecretValue $secretNoEnc
$registrationApiSecret = Add-DiscoveryApiResourceRegistration -identityServiceUrl $commonConfig.identityService -fabricInstallerSecret $decryptedSecret

$webDeployParameters = Get-WebDeployParameters -discoveryConfig $discoveryConfig `
                   -commonConfig $commonConfig `
                   -userName $discoveryIisUser.UserName `
                   -registrationApiSecret $registrationApiSecret

Write-DosMessage -Level Information -Message "Enter credentials for app pool $($discoveryConfig.appPoolName)"
Publish-DosWebApplication -WebAppPackagePath $installPackagePath `
                          -WebDeployParameters $webDeployParameters `
                          -AppPoolName $discoveryConfig.appPoolName `
                          -AppName $discoveryConfig.appName `
                          -AppPoolCredential $discoveryIisUser.Credential `
                          -AuthenticationType  $authenticationType `
                          -WebDeploy

$discoveryBaseUrlParameter = $webDeployParameters | Where-Object {$_.Name -eq "Application Endpoint Address" }
Test-DiscoveryService -discoveryBaseUrl $discoveryBaseUrlParameter.Value
