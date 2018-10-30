# This script will be moved into the install-identity script
param(
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
    [ValidateScript({
        if (!(Test-Path $_)) {
            throw "Path $_ does not exist. Please enter valid path to the IdentitySearchProvider directory"
        }
        return $true
    })] 
    [string] $idpssDirectoryPath = "$PSScriptRoot"
)

# Import Fabric Install Utilities
$fabricInstallUtilities = ".\Fabric-Install-Utilities.psm1"
if (!(Test-Path $fabricInstallUtilities -PathType Leaf)) {
    Write-DosMessage -Level "Warning" -Message "Could not find fabric install utilities. Manually downloading and installing"
    Invoke-WebRequest -Uri https://raw.githubusercontent.com/HealthCatalyst/InstallScripts/master/common/Fabric-Install-Utilities.psm1 -Headers @{"Cache-Control" = "no-cache"} -OutFile $fabricInstallUtilities
}
#Import-Module -Name $fabricInstallUtilities -Force
Import-Module -Name "$PSScriptRoot\Install-IdPSS-Utilities.psm1" -Force
Import-Module -Name "$PSScriptRoot\Fabric-Install-Utilities.psm1" -Force

#$installSettings = Get-InstallationSettings -configSection "common" -installConfigPath ".\install.config"
    $installationConfig = [xml](Get-Content $installConfigPath)
    $tenantScope = $installationConfig.installation.settings.scope | Where-Object {$_.name -eq "common"}
    $tenants = $tenantScope.SelectSingleNode('tenants')

$clientSettings = @()
foreach($tenant in $tenants.ChildNodes) {
    $tenantSetting = @{
        clientId = $tenant.clientId
        clientSecret = $tenant.secret
        tenantId = $tenant.tenantId
    }
    $clientSettings += $tenantSetting
}

Set-IdentityAppSettings -appDirectory $idpssDirectoryPath -useAzure $true -clientSettings $clientSettings