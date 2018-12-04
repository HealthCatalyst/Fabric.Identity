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
	[string[]] $registerApps = ("Identity Service", "Identity Provider Search Service")
)

$fabricInstallUtilities = ".\Fabric-Install-Utilities.psm1"
if (!(Test-Path $fabricInstallUtilities -PathType Leaf)) {
    Write-DosMessage -Level "Warning" -Message "Could not find fabric install utilities. Manually downloading and installing"
    Invoke-WebRequest -Uri https://raw.githubusercontent.com/HealthCatalyst/InstallScripts/master/common/Fabric-Install-Utilities.psm1 -Headers @{"Cache-Control" = "no-cache"} -OutFile $fabricInstallUtilities
}
Import-Module -Name  ".\Install-IdPSS-Utilities.psm1", ".\Install-Identity-Utilities.psm1", $fabricInstallUtilities -Force

if ($registerApps -eq "Identity Service")
{
   $registerIdentity = $true
}
else
{
   $registerIdentity = $false
}

if ($registerApps -eq "Identity Provider Search Service")
{
   $registerIdPSS = $true
}
else
{
   $registerIdPSS = $false
}

$installSettingsScope = "identity"

$tenants = Get-Tenants -installConfigPath $installConfigPath
$replyUrls = Get-ReplyUrls -installConfigPath $installConfigPath
$appNameIdPSS = "Identity Provider Search Service"
$appNameIdentity = "Identity Service"

if ($registerIdPSS)
{
   #IdentityProviderSearchService registration
   if($null -ne $tenants) {
      foreach($tenant in $tenants) { 
        Write-Host "Enter credentials for $appNameIdPSS specified tenant: $tenant"
        Connect-AzureADTenant -tenantId $tenant
		
        $app = New-FabricAzureADApplication -appName $appNameIdPSS -replyUrls $replyUrls
        $clientId = $app.AppId
        $clientSecret = Get-FabricAzureADSecret -objectId $app.ObjectId

        Disconnect-AzureAD
        Add-InstallationTenantSettings -configSection $installSettingsScope `
            -tenantId $tenant `
            -clientSecret $clientSecret `
            -clientId $clientId `
            -installConfigPath $installConfigPath `
			-appName $appNameIdPSS

        # Manual process, need to give consent this way for now
        Start-Process -FilePath  "https://login.microsoftonline.com/$tenant/oauth2/authorize?client_id=$clientId&response_type=code&state=12345&prompt=admin_consent"
      }
   }
}

if ($registerIdentity)
{
  #identity registration
  Register-Identity -appName $appNameIdentity -replyUrls $replyUrls -configSection $installSettingsScope -installConfigPath $installConfigPath
}

