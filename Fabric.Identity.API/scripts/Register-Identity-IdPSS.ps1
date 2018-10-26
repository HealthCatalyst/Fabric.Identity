Import-Module -Name $PSScriptRoot\Install-IdPSS-Utilities.psm1 -Force

function Get-InstallationConfig {
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
        [string] $installConfigPath = "install.config"
    )
    $installConfig = [xml](Get-Content $installConfigPath)

    return $installConfig
}
function Add-InstallationConfigSetting {
    param(
        [Parameter(Mandatory=$true)]
        [string] $configSetting,
        [Parameter(Mandatory=$true)]
        [string] $configValue,
        [Parameter(Mandatory=$true)]
        [string] $configClientId,
        [ValidateScript({
            if (!(Test-Path $_)) {
                throw "Path $_ does not exist. Please enter valid path to the install.config."
            }
            if (!(Test-Path $_ -PathType Leaf)) {
                throw "Path $_ is not a file. Please enter a valid path to the install.config."
            }
            return $true
        })]  
        [string] $installConfigPath = "install.config"
    )

    $installationConfig = [xml](Get-Content $installConfigPath)
    $tenantScope = $installationConfig.installation.settings.scope | Where-Object {$_.name -eq "common"}
    $tenantSettings = $tenantScope.tenants
    $existingSetting = $tenantSettings.variable | Where-Object {$_.name -eq $configSetting}

    if ($null -eq $existingSetting) {
        $setting = $installationConfig.CreateElement("variable")
        
        $nameAttribute = $installationConfig.CreateAttribute("name")
        $nameAttribute.Value = $configSetting
        $setting.Attributes.Append($nameAttribute)

        $valueAttribute = $installationConfig.CreateAttribute("value")
        $valueAttribute.Value = $configValue
        $setting.Attributes.Append($valueAttribute)

        $clientAttribute = $installationConfig.CreateAttribute("clientid")
        $clientAttribute.Value = $configClientId
        $setting.Attributes.Append($clientAttribute)

        $tenantSettings.AppendChild($setting)
    }
    else{
        $existingSetting.secret = $configValue
        $existingSetting.clientId = $configClientId
    }
    $installationConfig.Save("$installConfigPath")
}

$installConfigPath = "$PSScriptRoot\registration.config"

$installSettings = Get-InstallationConfig -installConfigPath $installConfigPath
#$tenants = $installSettings.registration.settings.tenants.variable
$commonScope = $installSettings.installation.settings.scope | Where-Object {$_.name -eq "common"}
$tenants = $commonScope.tenants.variable


# TODO: Differentiate between idpss and identity application for extra permissions
if($null -ne $tenants) {
    foreach($tenant in $tenants.name) {
        Write-Host "Enter credentials for specified tenant $tenant"
        $credential = Get-Credential

        #New-FabricAzureADApplicationRegistration -tenantId $tenant -credentials $credential

        Connect-AzureADTenant -tenantId $tenant -credentials $credential
        $app = New-FabricAzureADApplication
        $clientId = $app.AppId
        $clientSecret = Get-FabricAzureADSecret -objectId $app.ObjectId

        Disconnect-AzureAD

        Add-InstallationConfigSetting $tenant $clientSecret $clientId $installConfigPath

        # Manual process, need to give consent this way for now
        #Start-Process -FilePath  "https://login.microsoftonline.com/4d07d6d8-58e4-45a4-8ce9-5d2cfc00c65f/oauth2/authorize?client_id=e4fd028e-51ac-4c69-aee6-de0519566f5b&response_type=code&state=12345&prompt=admin_consent"
    }
}