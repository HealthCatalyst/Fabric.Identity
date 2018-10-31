$fabricInstallUtilities = ".\Install-Identity-Utilities.psm1"
Import-Module -Name $fabricInstallUtilities -Force

# Import AzureAD
$minVersion = [System.Version]::new(2, 0, 2 , 4)
$azureAD = Get-Childitem -Path ./**/AzureAD.psm1 -Recurse
if ($azureAD.length -eq 0) {
    $installed = Get-Module -Name AzureAD
    if ($null -eq $installed) {
        $installed = Get-InstalledModule -Name AzureAD
    }

    if (($null -eq $installed) -or ($installed.Version.CompareTo($minVersion) -lt 0)) {
        Write-Host "Installing AzureAD from Powershell Gallery"
        Install-Module AzureAD -Scope CurrentUser -MinimumVersion $minVersion -Force
        Import-Module AzureAD -Force
    }
}
else {
    Write-Host "Installing AzureAD at $($azureAD.FullName)"
    Import-Module -Name $azureAD.FullName
}

function Get-GraphApiReadPermissions() {
    $aad = (Get-AzureADServicePrincipal | Where-Object {$_.ServicePrincipalNames.Contains("https://graph.microsoft.com")})[0]
    $groupRead = $aad.Oauth2Permissions | Where-Object {$_.Value -eq "Group.Read.All"}
    $userRead = $aad.Oauth2Permissions | Where-Object {$_.Value -eq "User.Read.All"}

    # Convert to proper resource...
    $readAccess = [Microsoft.Open.AzureAD.Model.RequiredResourceAccess]@{
        ResourceAppId = $aad.AppId;
        ResourceAccess = [Microsoft.Open.AzureAD.Model.ResourceAccess]@{
            Id = $groupRead.Id;
            Type = "Role"
        },
        [Microsoft.Open.AzureAD.Model.ResourceAccess]@{
            Id = $userRead.Id;
            Type = "Role"
        }
    }

    return $readAccess
}

function New-FabricAzureADApplication() {
    param(
        [Parameter(Mandatory=$true)]
        [string] $appName,
        [Parameter(Mandatory=$true)]
        [string[]] $replyUrls
    )
    # Get read permissions
    $readResource = Get-GraphApiReadPermissions

    $app = Get-AzureADApplication -Filter "DisplayName eq '$appName'" -Top 1
    if($null -eq $app) {
        $app = New-AzureADApplication -Oauth2AllowImplicitFlow $true -RequiredResourceAccess $readResource -DisplayName $appName -ReplyUrls $replyUrls
    }
    else {
        Set-AzureADApplication -ObjectId $app.ObjectId -RequiredResourceAccess $readResource -Oauth2AllowImplicitFlow $true -ReplyUrls $replyUrls
    }

    return $app
}

function Get-FabricAzureADSecret([string] $objectId) {
    # TODO: Remove and recreate on every install?
    $credential = Get-AzureADApplicationPasswordCredential -ObjectId $objectId
    if($null -eq $credential) {
        $credential = New-AzureADApplicationPasswordCredential -ObjectId $objectId
    }

    return $credential.KeyId
}

function Connect-AzureADTenant {
    param(
        [Parameter(Mandatory=$true)]
        [string] $tenantId,
        [Parameter(Mandatory=$true)]
        [PSCredential] $credential
    )

    try {
        Connect-AzureAD -Credential $credential -TenantId $tenantId | Out-Null
    }
    catch {
        Write-DosMessage -Level "Error" -Message  "Could not sign into tenant '$tenantId' with user '$($credential.UserName)'"
        throw
    }
}

function Add-InstallationTenantSettings {
    param(
        [Parameter(Mandatory=$true)]
        [string] $configSection,
        [Parameter(Mandatory=$true)]
        [string] $tenantId,
        [Parameter(Mandatory=$true)]
        [string] $clientSecret,
        [Parameter(Mandatory=$true)]
        [string] $clientId,
        [ValidateScript({
            if (!(Test-Path $_)) {
                throw "Path $_ does not exist. Please enter valid path to the install.config."
            }
            if (!(Test-Path $_ -PathType Leaf)) {
                throw "Path $_ is not a file. Please enter a valid path to the install.config."
            }
            return $true
        })]  
        [string] $installConfigPath = "$(Get-CurrentScriptDirectory)\install.config"
    )

    $installationConfig = [xml](Get-Content $installConfigPath)
    $tenantScope = $installationConfig.installation.settings.scope | Where-Object {$_.name -eq $configSection}
    $tenantSettings = $tenantScope.SelectSingleNode('tenants')

    # Add a tenant section if not exists
    if($null -eq $tenantSettings) {
        $tenantSettings = $installationConfig.CreateElement("tenants")
        $tenantScope.AppendChild($tenantSettings) | Out-Null
    }

    $existingSetting = $tenantSettings.ChildNodes | Where-Object {$_.tenantId -eq $tenantId}

    if ($null -eq $existingSetting) {
        $setting = $installationConfig.CreateElement("variable")

        $nameAttribute = $installationConfig.CreateAttribute("tenantId")
        $nameAttribute.Value = $tenantId
        $setting.Attributes.Append($nameAttribute) | Out-Null

        $valueAttribute = $installationConfig.CreateAttribute("secret")
        $valueAttribute.Value = $clientSecret
        $setting.Attributes.Append($valueAttribute) | Out-Null

        $clientAttribute = $installationConfig.CreateAttribute("clientid")
        $clientAttribute.Value = $clientId
        $setting.Attributes.Append($clientAttribute) | Out-Null

        $tenantSettings.AppendChild($setting) | Out-Null
    }
    else{
        $existingSetting.secret = $clientSecret
        $existingSetting.clientId = $clientId
    }
    $installationConfig.Save("$installConfigPath") | Out-Null
}

function Set-AppSettings($appDirectory, $appSettings){
    Write-Host "Writing app settings to config..."
    $webConfig = [xml](Get-Content $appDirectory\web.config)
    foreach ($variable in $appSettings.GetEnumerator()){
        Add-AppSetting $variable.Name $variable.Value $webConfig
    }

    $webConfig.Save("$appDirectory\web.config")
}

function Add-AppSetting($appSettingName, $appSettingValue, $config){
    $appSettingsNode = $config.configuration.appSettings
    $existingAppSettings = $appSettingsNode.add | Where-Object {$_.key -eq $appSettingName}
    if($null -eq $existingAppSettings){
        Write-Host "Writing $appSettingName to config"
        $addElement = $config.CreateElement("add")
        
        $keyAttribute = $config.CreateAttribute("key")
        $keyAttribute.Value = $appSettingName
        $addElement.Attributes.Append($keyAttribute)
        
        $valueAttribute = $config.CreateAttribute("value")
        $valueAttribute.Value = $appSettingValue
        $addElement.Attributes.Append($valueAttribute)

        $appSettingsNode.AppendChild($addElement)
    }else {
        Write-Host $appSettingName "already exists in config, updating value"
        $existingAppSettings.Value = $appSettingValue
    }
}

function Get-ClientSettingsFromInstallConfig{
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
        [string] $installConfigPath
    )
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

    return $clientSettings
}

function Set-IdentityAppSettings {
    #[string] $primarySigningCertificateThumbprint, `
    #[string] $encryptionCertificateThumbprint, `
    #[string] $appInsightsInstrumentationKey, `
    param(
        [string] $appDirectory,
        [object[]] $clientSettings,
        [string] $useAzure = $false
    )
    $appSettings = @{}

    # TODO: Implement encryption for secrets
    # if ($primarySigningCertificateThumbprint){
    #     $appSettings.Add("SigningCertificateSettings__UseTemporarySigningCredential", "false")
    #     $appSettings.Add("SigningCertificateSettings__PrimaryCertificateThumbprint", $primarySigningCertificateThumbprint)
    # }

    # if ($encryptionCertificateThumbprint){
    #     $appSettings.Add("SigningCertificateSettings__EncryptionCertificateThumbprint", $encryptionCertificateThumbprint)
    # }

    # if($appInsightsInstrumentationKey){
    #     $appSettings.Add("ApplicationInsights__Enabled", "true")
    #     $appSettings.Add("ApplicationInsights__InstrumentationKey", $appInsightsInstrumentationKey)
    # }

    # TODO: Flag to enable
    if($useAzure -eq $true) {
        $defaultScope = "https://graph.microsoft.com/.default"
        # Azure Ad Setting
        $appSettings.Add("AzureActiveDirectoryClientSettings:Authority", "https://login.microsoftonline.com/")
        $appSettings.Add("AzureActiveDirectoryClientSettings:TokenEndpoint", "/oauth2/v2.0/token")
        $appSettings.Add("UseAzureAuthentication", "true")

        foreach($setting in $clientSettings) {
            $index = $clientSettings.IndexOf($setting)
            $appSettings.Add("AzureActiveDirectoryClientSettings:ClientAppSettings:$index`:ClientId", $setting.clientId)
            $appSettings.Add("AzureActiveDirectoryClientSettings:ClientAppSettings:$index`:ClientSecret", $setting.clientSecret)
            $appSettings.Add("AzureActiveDirectoryClientSettings:ClientAppSettings:$index`:TenantId", $setting.tenantId)
            
            # Currently only a single default scope is expected
            $appSettings.Add("AzureActiveDirectoryClientSettings:ClientAppSettings:$index`:Scopes:0", $defaultScope)
        }
    }
    Set-AppSettings $appDirectory $appSettings | Out-Null
}

Export-ModuleMember Set-IdentityAppSettings
Export-ModuleMember Add-EnvironmentVariable
Export-ModuleMember Get-FabricAzureADSecret
Export-ModuleMember Connect-AzureADTenant
Export-ModuleMember New-FabricAzureADApplication
Export-ModuleMember Add-InstallationTenantSettings
Export-ModuleMember Get-ClientSettingsFromInstallConfig