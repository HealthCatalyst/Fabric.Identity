$fabricInstallUtilities = ".\Install-Identity-Utilities.psm1"
Import-Module -Name $fabricInstallUtilities -Force

# Import AzureAD
$minVersion = [System.Version]::new(2, 0, 2 , 4)
$azureAD = Get-Childitem -Path ./**/AzureAD.psm1 -Recurse
if ($azureAD.length -eq 0) {
    try{
        $installed = Get-Module -Name AzureAD
    }
    catch {
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

function Remove-AzureADClientSecret{
    param(
        [string] $objectId,
        [string] $keyIdentifier
    )
    $encoding = [System.Text.Encoding]::ASCII
    $keys = Get-AzureADApplicationPasswordCredential -ObjectId $objectId
    $filteredKeys = $keys | Where-Object {$null -ne $_.CustomKeyIdentifier -and $encoding.GetString($_.CustomKeyIdentifier) -eq $keyIdentifier}
    foreach($key in $filteredKeys) {
        Write-Host "Removing existing password credential named `"$($encoding.GetString($key.CustomKeyIdentifier))`" with id $($key.KeyId)"
        Remove-AzureADApplicationPasswordCredential -ObjectId $objectId -KeyId $key.KeyId
    }
}

function Get-FabricAzureADSecret([string] $objectId) {
    # Cleanup existing secret
    $keyCredentialName = "PowerShell Created Password"
    Remove-AzureADClientSecret -objectId $objectId -keyIdentifier $keyCredentialName
    Write-Host "Creating password credential named $keyCredentialName"
    $credential = New-AzureADApplicationPasswordCredential -ObjectId $objectId -CustomKeyIdentifier $keyCredentialName
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
		[string] $appName
    )
    $installationConfig = [xml](Get-Content $installConfigPath)
    $identityScope = $installationConfig.installation.settings.scope | Where-Object {$_.name -eq $configSection}
    $applicationSettings = $identityScope.SelectSingleNode('registeredApplications')

    # Add a application section if not exists
    if($null -eq $applicationSettings) {
        $applicationSettings = $installationConfig.CreateElement("registeredApplications")
        $identityScope.AppendChild($applicationSettings) | Out-Null
    }

    $existingSetting = $applicationSettings.ChildNodes | Where-Object {$_.appName -eq $appName -and $_.tenantId -eq $tenantId}

    if ($null -eq $existingSetting) {
        $setting = $installationConfig.CreateElement("variable")

	    $appNameAttribute = $installationConfig.CreateAttribute("appName")
        $appNameAttribute.Value = $appName
        $setting.Attributes.Append($appNameAttribute) | Out-Null

        $nameAttribute = $installationConfig.CreateAttribute("tenantId")
        $nameAttribute.Value = $tenantId
        $setting.Attributes.Append($nameAttribute) | Out-Null

        $clientAttribute = $installationConfig.CreateAttribute("clientid")
        $clientAttribute.Value = $clientId
        $setting.Attributes.Append($clientAttribute) | Out-Null

        $valueAttribute = $installationConfig.CreateAttribute("secret")
        $valueAttribute.Value = $clientSecret
        $setting.Attributes.Append($valueAttribute) | Out-Null

        $applicationSettings.AppendChild($setting) | Out-Null
    }
    else{
        $existingSetting.secret = $clientSecret
        $existingSetting.clientId = $clientId
    }
    $installationConfig.Save("$installConfigPath") | Out-Null
}

function Set-WebConfigAppSettings($webConfigPath, $appSettings){
    Write-Host "Writing app settings to web config..."
    $webConfig = [xml](Get-Content $webConfigPath)
    foreach ($variable in $appSettings.GetEnumerator()){
        Add-AppSetting $variable.Name $variable.Value $webConfig
    }

    $webConfig.Save("$webConfigPath")
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
        [string] $installConfigPath,
        [string] $appName
    )
    $installationConfig = [xml](Get-Content $installConfigPath)
    $tenantScope = $installationConfig.installation.settings.scope | Where-Object {$_.name -eq "identity"}
    $tenants = $tenantScope.SelectSingleNode('registeredApplications')

    $clientSettings = @()
    foreach($tenant in $tenants.variable) {
      if ($tenant.appName -eq $appName)
      {
        $tenantSetting = @{
            clientId = $tenant.clientId
            # Does not decrypt secret
            clientSecret = $tenant.secret
            tenantId = $tenant.tenantId
        }
        $clientSettings += $tenantSetting
      }
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

function Clear-IdentityProviderSearchServiceWebConfigAzureSettings {
    param(
        [string] $webConfigPath
    )
    $content = [xml](Get-Content $webConfigPath)
    $settings = $content.configuration.appSettings

    $azureSettings = ($settings.ChildNodes | Where-Object {$null -ne $_.Key -and $_.Key.StartsWith("AzureActiveDirectoryClientSettings")})
    
    foreach($setting in $azureSettings) {
        Write-Host "Cleaning up setting: $($setting.key)"
        $settings.RemoveChild($setting) | Out-Null
    }
    $content.Save("$webConfigPath")

}

function Clear-IdentityEnvironmentAzureSettings {
    param(
        [string] $environmentSettingPath
    )
    $content = [xml](Get-Content $environmentSettingPath)
    $settings = $content.configuration
    $environmentVariables = $settings.ChildNodes.aspNetCore.environmentVariables

    $azureSettings = ($environmentVariables.ChildNodes | Where-Object {$_.name.StartsWith("AzureActiveDirectorySettings")})
    
    foreach($setting in $azureSettings) {
        Write-Host "Cleaning up setting: $($setting.name)"
        $environmentVariables.RemoveChild($setting) | Out-Null
    }
    $content.Save("$environmentSettingPath")

}

function Set-IdentityProviderSearchServiceWebConfigSettings {
    param(
        [string] $encryptionCertificateThumbprint,
        [string] $appInsightsInstrumentationKey,
        [string] $webConfigPath,
        [string] $installConfigPath,
        [string] $useAzure = $false,
        [string] $useWindows = $true,
        [System.Security.Cryptography.X509Certificates.X509Certificate2] $encryptionCert,
        [string] $appName
    )

    # Alter IdPSS web.config for azure
    $clientSettings = @()
    $clientSettings += Get-ClientSettingsFromInstallConfig -installConfigPath $installConfigPath -appName $appName

    Clear-IdentityProviderSearchServiceWebConfigAzureSettings -webConfigPath $webConfigPath
    $appSettings = @{}
    if ($encryptionCertificateThumbprint){
        $appSettings.Add("EncryptionCertificateSettings:EncryptionCertificateThumbprint", $encryptionCertificateThumbprint)
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
        if($secret -is [string] -and !$secret.StartsWith("!!enc!!:")){
            $encryptedSecret = Get-EncryptedString  $encryptionCert $secret
            # Encrypt secret in install.config if not encrypted
            Add-InstallationTenantSettings -configSection "identity" `
                -tenantId $setting.tenantId `
                -clientSecret $encryptedSecret `
                -clientId $setting.clientId `
                -installConfigPath $installConfigPath `
                -appName $appName

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

    if($useWindows -eq $true) {
        $appSettings.Add("UseWindowsAuthentication", "true")
    }
    elseif($useWindows -eq $false) {
        $appSettings.Add("UseWindowsAuthentication", "false")
    }

    Set-WebConfigAppSettings $webConfigPath $appSettings | Out-Null
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
        Write-DosMessage -Level "Error" -Message  "No tenants to register where found in the install.config"
        throw
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
        Write-DosMessage -Level "Error" -Message  "No reply urls where found in the install.config."
        throw
    }

    return $replyUrls
}

function Register-Identity {
    param(
        [Parameter(Mandatory=$true)]
        [string] $appName,
		[Parameter(Mandatory=$true)]
        [string] $replyUrls,
		[Parameter(Mandatory=$true)]
        [string] $configSection,
		[Parameter(Mandatory=$true)]
        [string] $installConfigPath
    )
    $allowedTenantsText = "allowedTenants"
    $claimsIssuerText = "claimsIssuerTenant"
	$allowedTenants += Get-SettingsFromInstallConfig -installConfigPath $installConfigPath `
        -scope $configSection `
        -setting $allowedTenantsText

    $claimsIssuer += Get-SettingsFromInstallConfig -installConfigPath $installConfigPath `
        -scope $configSection `
        -setting $claimsIssuerText

   if($null -ne $claimsIssuer) {
    Write-Host "Enter credentials for $appName specified tenant: $claimsIssuer"
	Connect-AzureADTenant -tenantId $claimsIssuer

	$app = New-FabricAzureADApplication -appName $appName -replyUrls $replyUrls
	$clientId = $app.AppId
	$clientSecret = Get-FabricAzureADSecret -objectId $app.ObjectId
		 
	Disconnect-AzureAD

	Add-InstallationTenantSettings -configSection $configSection `
	-tenantId $claimsIssuer `
	-clientSecret $clientSecret `
	-clientId $clientId `
	-installConfigPath $installConfigPath `
	-appName $appName
  }
  else 
  {
    Write-DosMessage -Level "Information" -Message "No claims issuer tenant was found in the install.config."
  }
}

function Set-IdentityEnvironmentAzureVariables {
    param (
        [string] $appConfig,
        [string] $installConfigPath,
        [string] $useAzure = $false,
        [string] $useWindows = $true,
        [System.Security.Cryptography.X509Certificates.X509Certificate2] $encryptionCert
    )
    $scope = "identity"
	# Alter Identity web.config for azure
    $clientSettings = Get-ClientSettingsFromInstallConfig -installConfigPath $installConfigPath -appName "Identity Service"
    $allowedTenants += Get-SettingsFromInstallConfig -installConfigPath $installConfigPath `
        -scope $scope `
        -setting "allowedTenants"

    $claimsIssuer += Get-SettingsFromInstallConfig -installConfigPath $installConfigPath `
        -scope $scope `
        -setting "claimsIssuerTenant"

	Clear-IdentityEnvironmentAzureSettings -environmentSettingPath $appConfig\web.config
    $environmentVariables = @{}

	# Set Azure Settings
    $environmentVariables.Add("AzureActiveDirectorySettings_Authority", "https://login.microsoftonline.com/common")
    $environmentVariables.Add("AzureActiveDirectorySettings_DisplayName", "Azure AD")
	$environmentVariables.Add("AzureActiveDirectorySettings_ClaimsIssuer", "https://login.microsoftonline.com/" + $claimsIssuer)
    $environmentVariables.Add("AzureActiveDirectorySettings_Scope_0", "openid")
	$environmentVariables.Add("AzureActiveDirectorySettings_Scope_1", "profile")
    $environmentVariables.Add("AzureActiveDirectorySettings_ClientId", $clientSettings.clientId)

    $secret = $clientSettings.clientSecret
        if($secret -is [string] -and !$secret.StartsWith("!!enc!!:")){
            $encryptedSecret = Get-EncryptedString  $encryptionCert $secret
            # Encrypt secret in install.config if not encrypted
            Add-InstallationTenantSettings -configSection "identity" `
                -tenantId $clientSettings.tenantId `
                -clientSecret $encryptedSecret `
                -clientId $clientSettings.clientId `
                -installConfigPath $installConfigPath `
                -appName "Identity Service"

            $environmentVariables.Add("AzureActiveDirectorySettings_ClientSecret", $encryptedSecret)
        }
        else{
            $environmentVariables.Add("AzureActiveDirectorySettings_ClientSecret", $secret)
        }

    foreach($allowedTenant in $allowedTenants)
    {
      $index = $allowedTenants.IndexOf($allowedTenant)
      $environmentVariables.Add("AzureActiveDirectorySettings_IssuerWhiteList_$index", "https://sts.windows.net/" + $allowedTenant)
    }

    if($useAzure -eq $true) {
        $environmentVariables.Add("AzureAuthenticationEnabled", "true")
    }
    elseif($useAzure -eq $false) {
        $environmentVariables.Add("AzureAuthenticationEnabled", "false")
    }

    if($useWindows -eq $true) {
        $environmentVariables.Add("WindowsAuthenticationEnabled", "true")
    }
    elseif($useWindows -eq $false) {
        $environmentVariables.Add("WindowsAuthenticationEnabled", "false")
    }

    Set-EnvironmentVariables $appConfig $environmentVariables | Out-Null
}

Export-ModuleMember Set-IdentityProviderSearchServiceWebConfigSettings
Export-ModuleMember Set-IdentityEnvironmentAzureVariables
Export-ModuleMember Get-FabricAzureADSecret
Export-ModuleMember Connect-AzureADTenant
Export-ModuleMember New-FabricAzureADApplication
Export-ModuleMember Add-InstallationTenantSettings
Export-ModuleMember Get-ClientSettingsFromInstallConfig
Export-ModuleMember Get-SettingsFromInstallConfig
Export-ModuleMember Get-Tenants
Export-ModuleMember Get-ReplyUrls
Export-ModuleMember Register-Identity