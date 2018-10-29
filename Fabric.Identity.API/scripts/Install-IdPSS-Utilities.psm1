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
        # TODO: if does not exist, create, else update
    if($null -eq $app) {
        # create new application
        # TODO: Add redirect url
        $app = New-AzureADApplication -Oauth2AllowImplicitFlow $true -RequiredResourceAccess $readResource -DisplayName $appName -ReplyUrls $replyUrls
    }
    else {
        # TODO: Add redirect url
        Set-AzureADApplication -ObjectId $app.ObjectId -RequiredResourceAccess $readResource -Oauth2AllowImplicitFlow $true 
    }

    return $app
    # TODO: Store the $app.AppId as the client-id
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
        [PSCredential] $credentials
    )
    # Step 1. Connect to azure tenant
    try {
        Connect-AzureAD -Credential $credentials -TenantId $tenantId | Out-Null
    }
    catch {
        Write-DosMessage -Level "Error" -Message  "Could not sign into tenant '$tenantId' with user '$($credentials.UserName)'"
        throw $_.Exception
    }
}
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

Export-ModuleMember Get-FabricAzureADSecret
Export-ModuleMember Connect-AzureADTenant
Export-ModuleMember New-FabricAzureADApplication
Export-ModuleMember Add-InstallationConfigSetting
Export-ModuleMember Get-InstallationConfig