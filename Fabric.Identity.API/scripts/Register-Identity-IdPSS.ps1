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
    [string] $azureConfigPath = "$PSScriptRoot\azuresettings.config",
    [switch] $registerIdentity,
    [switch] $registerIdPSS
)

if(!($registerIdentity) -and !($registerIdPSS))
{
	throw " You must register either Identity and/or IdPSS by adding their respective switch.
	Examples:
		.\Register-Identity-IdPSS.ps1 -registerIdentity
		.\Register-Identity-IdPSS.ps1 -registerIdPSS
		.\Register-Identity-IdPSS.ps1 -registerIdentity -registerIdPSS"
}

$fabricInstallUtilities = ".\Fabric-Install-Utilities.psm1"
if (!(Test-Path $fabricInstallUtilities -PathType Leaf)) {
    Write-DosMessage -Level "Warning" -Message "Could not find fabric install utilities. Manually downloading and installing"
    Invoke-WebRequest -Uri https://raw.githubusercontent.com/HealthCatalyst/InstallScripts/master/common/Fabric-Install-Utilities.psm1 -Headers @{"Cache-Control" = "no-cache"} -OutFile $fabricInstallUtilities
}
Import-Module -Name  ".\Install-IdPSS-Utilities.psm1", ".\Install-Identity-Utilities.psm1", $fabricInstallUtilities -Force

$installSettingsScope = "identity"

$tenants = Get-Tenants -azureConfigPath $azureConfigPath
$replyUrls = Get-ReplyUrls -azureConfigPath $azureConfigPath
$appNameIdPSS = $Global:idPSSAppName
$appNameIdentity = "Identity Service"

if ($registerIdPSS)
{
   Register-IdPSS -appName $appNameIdPSS -replyUrls $replyUrls -tenants $tenants -configSection $installSettingsScope -azureConfigPath $azureConfigPath
}

if ($registerIdentity)
{
  # Identity registration
  Register-Identity -appName $appNameIdentity -replyUrls $replyUrls -configSection $installSettingsScope -azureConfigPath $azureConfigPath
}

