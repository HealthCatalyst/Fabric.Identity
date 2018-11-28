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
    [string] $installConfigPath = "$PSScriptRoot\install.config"
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

if($null -ne $tenants) {
    foreach($tenant in $tenants) { 
        Write-Host "Enter credentials for specified tenant $tenant"
        Connect-AzureADTenant -tenantId $tenant

        $app = New-FabricAzureADApplication -appName 'Identity Provider Search Service' -replyUrls $replyUrls
        $clientId = $app.AppId
        $clientSecret = Get-FabricAzureADSecret -objectId $app.ObjectId

        Disconnect-AzureAD
        Add-InstallationTenantSettings -configSection $installSettingsScope `
            -tenantId $tenant `
            -clientSecret $clientSecret `
            -clientId $clientId `
            -installConfigPath $installConfigPath

        # Manual process, need to give consent this way for now
        Start-Process -FilePath  "https://login.microsoftonline.com/$tenant/oauth2/authorize?client_id=$clientId&response_type=code&state=12345&prompt=admin_consent"
    }
}
