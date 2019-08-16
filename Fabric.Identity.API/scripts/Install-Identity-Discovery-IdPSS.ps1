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
    [string] $azureConfigPath = "$env:ProgramFiles\Health Catalyst\azuresettings.config",
    [string] $migrationInstallConfigPath = "$env:ProgramFiles\Health Catalyst\install.config",
    [string] $migrationAzureConfigPath = "$PSScriptRoot\azuresettings.config",
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
$azureConfigStore = @{Type = "File"; Format = "XML"; Path = "$azureConfigPath"}

# Identity Settings
$discoveryScope = "discoveryservice"
$discoveryConfig = Get-DosConfigValues -ConfigStore $configStore -Scope $discoveryScope
$enableOAuth = [string]::IsNullOrEmpty($discoveryConfig.enableOAuth) -ne $true -and $discoveryConfig.enableOAuth -eq "true"

# Check if install.config has AAD settings and azureConfigStore is empty
# update azureConfigStore with the newly created config file.
$existingAzurePath = Test-Path $azureConfigPath -PathType Leaf
if($false -eq $existingAzurePath)
{
  $existingInstallPath = Test-Path $migrationInstallConfigPath -PathType Leaf

  # Need to allow verbose logging to work for debugging
  $commonScope = "common"
  $migrationInstallConfigStore = @{Type = "File"; Format = "XML"; Path = "$migrationInstallConfigPath"}
  $commonConfig = Get-DosConfigValues -ConfigStore $migrationInstallConfigStore -Scope $commonScope  
  Set-DosMessageConfiguration -LoggingMode Both -MinimumLoggingLevel $commonConfig.minimumLoggingLevel -LogFilePath $commonConfig.logFilePath

  # quick check in Migrate-AADSettings to know if there are AAD Settings
  if($true -eq $existingInstallPath)
  {
   Write-DosMessage -Level "Information" -Message "Started the Migration of AAD Settings from install.config to azuresettings.config"
   
   $nodesToSearch = @("tenants", "replyUrls", "claimsIssuerTenant", "allowedTenants", "registeredApplications", "secretName")
   $ranMigration = Migrate-AADSettings -installConfigPath $migrationInstallConfigPath -azureConfigPath $migrationAzureConfigPath -nodesToSearch $nodesToSearch
   # add azuresettings.config back to Program Files/Health Catalyst
   if($ranMigration)
   {
    Copy-Item $migrationAzureConfigPath -Destination $azureConfigPath
    Write-DosMessage -Level "Verbose" -Message "Copied azuresettings.config to $($azureConfigPath)"
   }

   Write-DosMessage -Level "Information" -Message "Completed the Migration of AAD Settings from install.config to azuresettings.config"
  }
}

# Call the Identity powershell script
.\Install-Identity.ps1 -credential $credential -configStore $configStore -azureConfigStore $azureConfigStore -noDiscoveryService:$noDiscoveryService -quiet:$quiet
Write-DosMessage -Level "Information" -Message "Fabric.Identity has been installed."

# Call the Idpss powershell script
.\Install-IdentityProviderSearchService.ps1 -credential $credential -configStore $configStore -azureConfigStore $azureConfigStore -noDiscoveryService:$noDiscoveryService -quiet:$quiet
Write-DosMessage -Level "Information" -Message "Fabric.Identity Provider Search Service has been installed."

.\Install-Discovery.ps1 -credential $discoveryServiceCredential -configStore $configStore -quiet:$quiet
Write-DosMessage -Level "Information" -Message "Discovery has been installed."

if(!$quiet){
    Read-Host -Prompt "Installation complete, press Enter to exit"
}
