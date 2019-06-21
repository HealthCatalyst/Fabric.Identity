#Requires -RunAsAdministrator
#Requires -Version 5.1
#Requires -Modules PowerShellGet, PackageManagement

param(
    [PSCredential] $credential,
    [PSCredential] $discoveryServiceCredential,
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

# common Settings
$configStore = @{Type = "File"; Format = "XML"; Path = "$installConfigPath"}

# Identity Settings
$discoveryScope = "discoveryservice"
$discoveryConfig = Get-DosConfigValues -ConfigStore $configStore -Scope $discoveryScope
$enableOAuth = [string]::IsNullOrEmpty($discoveryConfig.enableOAuth) -ne $true -and $discoveryConfig.enableOAuth -eq "true"

# Call the Identity powershell script
.\Install-Identity.ps1 -credential $credential -configStore $configStore -noDiscoveryService:$noDiscoveryService -quiet:$quiet
Write-DosMessage -Level "Information" -Message "Fabric.Identity has been installed."

# Call the Idpss powershell script
.\Install-IdentityProviderSearchService.ps1 -credential $credential -configStore $configStore -noDiscoveryService:$noDiscoveryService -quiet:$quiet
Write-DosMessage -Level "Information" -Message "Fabric.Identity Provider Search Service has been installed."

.\Install-Discovery.ps1 -credential $discoveryServiceCredential -configStore $configStore -quiet:$quiet
Write-DosMessage -Level "Information" -Message "Discovery has been installed."

if(!$quiet){
    Read-Host -Prompt "Installation complete, press Enter to exit"
}
