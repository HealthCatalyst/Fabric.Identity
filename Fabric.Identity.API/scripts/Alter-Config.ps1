Import-Module -Name $PSScriptRoot\Install-IdPSS-Utilities.psm1 -Force

function Get-AppSettings {
    param(
        [ValidateScript({
            if (!(Test-Path $_)) {
                throw "Path $_ does not exist. Please enter valid path to the appsettings.json."
            }
            if (!(Test-Path $_ -PathType Leaf)) {
                throw "Path $_ is not a file. Please enter a valid path to the appsettings.json."
            }
            return $true
        })]  
        [string] $installConfigPath = "appsettings.json"
    )
    $appSettings = Get-Content $installConfigPath -raw | ConvertFrom-Json

    return $appSettings
}

$appSettingsPath = "$PSScriptRoot\appsettings.json"
$appSettings = Get-AppSettings $appSettingsPath

$installConfigPath = "$PSScriptRoot\registration.config"
$installSettings = Get-InstallationConfig -installConfigPath $installConfigPath
$commonScope = $installSettings.installation.settings.scope | Where-Object {$_.name -eq "common"}
$tenants = $commonScope.tenants.variable

$newSettings = @()

if($null -ne $tenants) {
    foreach($tenant in $tenants) {
        $body = @{
            ClientId = $tenant.clientid
            ClientSecret = $tenant.secret
            TenantId = $tenant.name
            Scopes = @("https://graph.microsoft.com/.default")
        }
        $newSettings += $body
    }
}

$appSettings.AzureActiveDirectoryClientSettings.ClientAppSettings = $newSettings

$appSettings | ConvertTo-Json -Depth 5 | Set-Content $appSettingsPath