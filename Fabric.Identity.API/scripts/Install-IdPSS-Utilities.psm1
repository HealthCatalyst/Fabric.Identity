$fabricInstallUtilities = ".\Install-Identity-Utilities.psm1"
Import-Module -Name $fabricInstallUtilities -Force

# Import AzureAD
$minVersion = [System.Version]::new(2, 0, 2 , 4)
$azureAD = Get-Childitem -Path ./**/AzureAD.psm1 -Recurse
if ($azureAD.length -eq 0) {
    # Do not show error when AzureAD is not installed, will install instead
    $installed = Get-InstalledModule -Name AzureAD -ErrorAction "silentlycontinue"

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

Export-ModuleMember Get-FabricAzureADSecret
Export-ModuleMember Connect-AzureADTenant
Export-ModuleMember New-FabricAzureADApplication
Export-ModuleMember Get-Tenants
Export-ModuleMember Get-ReplyUrls
Export-ModuleMember Register-Identity