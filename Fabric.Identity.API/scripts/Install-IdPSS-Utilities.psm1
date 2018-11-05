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
    $directoryRead = $aad.AppRoles | Where-Object {$_.Value -eq "Directory.Read.All"}

    # Convert to proper resource...
    $readAccess = [Microsoft.Open.AzureAD.Model.RequiredResourceAccess]@{
        ResourceAppId = $aad.AppId;
        ResourceAccess = [Microsoft.Open.AzureAD.Model.ResourceAccess]@{
            Id = $directoryRead.Id;
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
    $credential = New-AzureADApplicationPasswordCredential -ObjectId $objectId
    return $credential.Value
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
        [string] $installConfigPath = "$(Get-CurrentScriptDirectory)\install.config",
        [System.Security.Cryptography.X509Certificates.X509Certificate2] $signingCertificate
    )

    $installationConfig = [xml](Get-Content $installConfigPath)
    $tenantScope = $installationConfig.installation.settings.scope | Where-Object {$_.name -eq $configSection}
    $tenantSettings = $tenantScope.SelectSingleNode('registeredApplications')

    # Add a tenant section if not exists
    if($null -eq $tenantSettings) {
        $tenantSettings = $installationConfig.CreateElement("registeredApplications")
        $tenantScope.AppendChild($tenantSettings) | Out-Null
    }

    $existingSetting = $tenantSettings.ChildNodes | Where-Object {$_.tenantId -eq $tenantId}
    $encryptedSecret = Get-EncryptedString $signingCertificate $clientSecret

    if ($null -eq $existingSetting) {
        $setting = $installationConfig.CreateElement("variable")

        $nameAttribute = $installationConfig.CreateAttribute("tenantId")
        $nameAttribute.Value = $tenantId
        $setting.Attributes.Append($nameAttribute) | Out-Null

        $clientAttribute = $installationConfig.CreateAttribute("clientid")
        $clientAttribute.Value = $clientId
        $setting.Attributes.Append($clientAttribute) | Out-Null

        $valueAttribute = $installationConfig.CreateAttribute("secret")
        $valueAttribute.Value = $encryptedSecret
        $setting.Attributes.Append($valueAttribute) | Out-Null

        $tenantSettings.AppendChild($setting) | Out-Null
    }
    else{
        $existingSetting.secret = $encryptedSecret
        $existingSetting.clientId = $clientId
    }
    $installationConfig.Save("$installConfigPath") | Out-Null
}

function Set-AppSettings($appConfig, $appSettings){
    Write-Host "Writing app settings to config..."
    $webConfig = [xml](Get-Content $appConfig)
    foreach ($variable in $appSettings.GetEnumerator()){
        Add-AppSetting $variable.Name $variable.Value $webConfig
    }

    $webConfig.Save("$appConfig")
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
    $tenantScope = $installationConfig.installation.settings.scope | Where-Object {$_.name -eq "identity"}
    $tenants = $tenantScope.SelectSingleNode('registeredApplications')

    $clientSettings = @()
    foreach($tenant in $tenants.ChildNodes) {
        $tenantSetting = @{
            clientId = $tenant.clientId
            # Does not decrypt secret
            clientSecret = $tenant.secret
            tenantId = $tenant.tenantId
        }
        $clientSettings += $tenantSetting
    }

    return $clientSettings
}

function Get-SettingsFromInstallConfig{
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
        [string] $installConfigPath,
        [string] $scope,
        [string] $setting
    )
    $installationConfig = [xml](Get-Content $installConfigPath)
    $tenantScope = $installationConfig.installation.settings.scope | Where-Object {$_.name -eq $scope}
    $tempNode = $tenantScope.SelectSingleNode($setting)
    $settingList = @()
    foreach($nodeChild in $tempNode.variable){
        if($nodeChild.name) {
            $settingList += $nodeChild.name
        }
    }
    return $settingList
}

function Set-IdentityAppSettings {
    param(
        [string] $primarySigningCertificateThumbprint,
        [string] $encryptionCertificateThumbprint,
        [string] $appInsightsInstrumentationKey,
        [string] $appConfig,
        [object[]] $clientSettings,
        [string] $useAzure = $false,
        [System.Security.Cryptography.X509Certificates.X509Certificate2] $encryptionCert
    )
    $appSettings = @{}

    if ($primarySigningCertificateThumbprint){
        $appSettings.Add("SigningCertificateSettings:UseTemporarySigningCredential", "false")
        $appSettings.Add("SigningCertificateSettings:PrimaryCertificateThumbprint", $primarySigningCertificateThumbprint)
    }

    if ($encryptionCertificateThumbprint){
        $appSettings.Add("SigningCertificateSettings:EncryptionCertificateThumbprint", $encryptionCertificateThumbprint)
    }

    if($appInsightsInstrumentationKey){
        $appSettings.Add("ApplicationInsights:Enabled", "true")
        $appSettings.Add("ApplicationInsights:InstrumentationKey", $appInsightsInstrumentationKey)
    }

    # Set Azure Settings
    $defaultScope = "https://graph.microsoft.com/.default"
    $appSettings.Add("AzureActiveDirectoryClientSettings:Authority", "https://login.microsoftonline.com/")
    $appSettings.Add("AzureActiveDirectoryClientSettings:TokenEndpoint", "/oauth2/v2.0/token")

    foreach($setting in $clientSettings) {
        $index = $clientSettings.IndexOf($setting)
        $appSettings.Add("AzureActiveDirectoryClientSettings:ClientAppSettings:$index`:ClientId", $setting.clientId)
        $appSettings.Add("AzureActiveDirectoryClientSettings:ClientAppSettings:$index`:TenantId", $setting.tenantId)
        
        # Currently only a single default scope is expected
        $appSettings.Add("AzureActiveDirectoryClientSettings:ClientAppSettings:$index`:Scopes:0", $defaultScope)

        $secret = $setting.clientSecret
        if($secret -is [string] -and -not $secret.StartsWith("!!enc!!:")){
            $encryptedSecret = Get-EncryptedString  $encryptionCert $secret
            $appSettings.Add("AzureActiveDirectoryClientSettings:ClientAppSettings:$index`:ClientSecret", $encryptedSecret)
        }
        else{
            $appSettings.Add("AzureActiveDirectoryClientSettings:ClientAppSettings:$index`:ClientSecret", $secret)
        }
    }

    if($useAzure -eq $true) {
        $appSettings.Add("UseAzureAuthentication", "true")
    }
    elseif($useAzure -eq $false) {
        $appSettings.Add("UseAzureAuthentication", "false")
    }

    Set-AppSettings $appConfig $appSettings | Out-Null
}

function Add-NestedSetting {
    param(
        [Parameter(Mandatory=$true)]
        [string] $configSection,
        [ValidateScript({
            if (!(Test-Path $_)) {
                throw "Path $_ does not exist. Please enter valid path to the install.config."
            }
            if (!(Test-Path $_ -PathType Leaf)) {
                throw "Path $_ is not a file. Please enter a valid path to the install.config."
            }
            return $true
        })]
        [string] $installConfigPath = "$(Get-CurrentScriptDirectory)\install.config",
        [Parameter(Mandatory=$true)]
        [string] $parentSetting,
        [Parameter(Mandatory=$true)]
        [string] $value
    )
    $installationConfig = [xml](Get-Content $installConfigPath)
    $scope = $installationConfig.installation.settings.scope | Where-Object {$_.name -eq $configSection}
    $settingNode = $scope.SelectSingleNode($parentSetting)

    # Add a section if not exists
    if($null -eq $settingNode) {
        $settingNode = $installationConfig.CreateElement($parentSetting)
        $scope.AppendChild($settingNode) | Out-Null
    }

    $existingSetting = $settingNode.Variable | Where-Object {$_.name -eq $value}

    if ($null -eq $existingSetting) {
        Write-Console "Writing $value to config"
        $settingElement = $installationConfig.CreateElement("variable")

        $nameAttribute = $installationConfig.CreateAttribute("name")
        $nameAttribute.Value = $value
        $settingElement.Attributes.Append($nameAttribute) | Out-Null

        $settingNode.AppendChild($settingElement) | Out-Null
    }
    else{
        Write-Host $value "already exists in config, not overwriting"
    }
    $installationConfig.Save("$installConfigPath") | Out-Null

}

function Get-Tenants {
    param(
        [string] $installConfigPath
    )
    $tenants = @()
    $scope = "identity"
    $parentSetting = "tenants"
    $tenants += Get-SettingsFromInstallConfig -installConfigPath $installConfigPath `
        -scope $scope `
        -setting $parentSetting

    if($null -eq $tenants -or $tenants.Count -eq 0){
        $failedAttempts = 1
        do {
            if($failedAttempts -gt 10){
                Write-DosMessage -Level "Error" -Message "No tenants were entered."
                throw
            }

            $input = Read-Host "Please enter tenant to register Identity with"
            if(-not [string]::IsNullOrEmpty($input)) {
                $tenants += $input
            }
            else {
                $failedAttempts++
            }
        } until ([string]::IsNullOrEmpty($input) -and $tenants.Count -ne 0)

        foreach($tenant in $tenants){
            Add-NestedSetting -configSection $scope `
                -installConfigPath $installConfigPath `
                -parentSetting $parentSetting `
                -value $tenant
        }
    }

    return $tenants
}

function Get-ReplyUrls {
    param(
        [string] $installConfigPath
    )
    $scope = "identity"
    $parentSetting = "replyUrls"
    $replyUrls = @()
    $replyUrls += Get-SettingsFromInstallConfig -installConfigPath $installConfigPath -scope $scope -setting $parentSetting

    if($null -eq $replyUrls -or $replyUrls.Count -eq 0){
        # Build default identity url
        $replyUrls += Get-ApplicationEndpoint -appName $scope `
            -applicationEndpoint $null `
            -installConfigPath $installConfigPath `
            -scope $scope `
            -quiet $true

        foreach($replyUrl in $replyUrls){
            Add-NestedSetting -configSection $scope `
                -installConfigPath $installConfigPath `
                -parentSetting $parentSetting `
                -value $replyUrl
        }
    }

    return $replyUrls
}

Export-ModuleMember Set-IdentityAppSettings
Export-ModuleMember Add-EnvironmentVariable
Export-ModuleMember Get-FabricAzureADSecret
Export-ModuleMember Connect-AzureADTenant
Export-ModuleMember New-FabricAzureADApplication
Export-ModuleMember Add-InstallationTenantSettings
Export-ModuleMember Get-ClientSettingsFromInstallConfig
Export-ModuleMember Get-SettingsFromInstallConfig
Export-ModuleMember Get-Tenants
Export-ModuleMember Get-ReplyUrls