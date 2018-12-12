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
    [switch] $registerIdentity,
    [switch] $registerIdPSS
)

$fabricInstallUtilities = ".\Fabric-Install-Utilities.psm1"
if (!(Test-Path $fabricInstallUtilities -PathType Leaf)) {
    Write-DosMessage -Level "Warning" -Message "Could not find fabric install utilities. Manually downloading and installing"
    Invoke-WebRequest -Uri https://raw.githubusercontent.com/HealthCatalyst/InstallScripts/master/common/Fabric-Install-Utilities.psm1 -Headers @{"Cache-Control" = "no-cache"} -OutFile $fabricInstallUtilities
}
Import-Module -Name  ".\Install-IdPSS-Utilities.psm1", ".\Install-Identity-Utilities.psm1", $fabricInstallUtilities -Force

$installSettingsScope = "identity"

$tenants = Get-Tenants -installConfigPath $installConfigPath
$replyUrls = Get-ReplyUrls -installConfigPath $installConfigPath
$appNameIdPSS = "Identity Provider Search Service"
$appNameIdentity = "Identity Service"

if ($registerIdPSS)
{
   Register-IdPSS -appName $appNameIdPSS -replyUrls $replyUrls -tenants $tenants -configSection $installSettingsScope -installConfigPath $installConfigPath
}

if ($registerIdentity)
{
  # Identity registration
  Register-Identity -appName $appNameIdentity -replyUrls $replyUrls -configSection $installSettingsScope -installConfigPath $installConfigPath
}

