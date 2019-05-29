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

function Get-GraphApiDirectoryReadPermissions() {
    try {
        $aad = @((Get-AzureADServicePrincipal -Filter "ServicePrincipalNames eq 'https://graph.microsoft.com'"))[0]
    }
    catch {
        Write-DosMessage -Level "Error" -Message "Was not able to get the Microsoft Graph API service principal."
        throw
    }

    $directoryReadName = "Directory.Read.All"
    $directoryRead = $aad.AppRoles | Where-Object {$_.Value -eq $directoryReadName}
    if($null -eq $directoryRead) {
        Write-DosMessage -Level "Error" -Message "Was not able to find permissions $directoryReadName."
        throw
    }

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

function Get-GraphApiUserReadPermissions() {
    try {
        $aad = @((Get-AzureADServicePrincipal -Filter "ServicePrincipalNames eq 'https://graph.microsoft.com'"))[0]
    }
    catch {
        Write-DosMessage -Level "Error" -Message "Was not able to get the Microsoft Graph API service principal."
        throw
    }

    $directoryReadName = "User.Read"
    $directoryRead = $aad.Oauth2Permissions | Where-Object {$_.Value -eq $directoryReadName}
    if($null -eq $directoryRead) {
        Write-DosMessage -Level "Error" -Message "Was not able to find permissions $directoryReadName."
        throw
    }

    # Convert to proper resource...
    $readAccess = [Microsoft.Open.AzureAD.Model.RequiredResourceAccess]@{
        ResourceAppId = $aad.AppId;
        ResourceAccess = [Microsoft.Open.AzureAD.Model.ResourceAccess]@{
            Id = $directoryRead.Id;
            Type = "Scope"
        }
    }

    return $readAccess
}


function New-FabricAzureADApplication() {
    param(
        [Parameter(Mandatory=$true)]
        [string] $appName,
        [Parameter(Mandatory=$true)]
        [Hashtable[]] $replyUrls,
        [Microsoft.Open.AzureAD.Model.RequiredResourceAccess] $permission,
        [bool] $isMultiTenant = $false
    )
    $groupMembershipClaims = 'SecurityGroup'

    $app = Get-AzureADApplication -Filter "DisplayName eq '$appName'" -Top 1
    if($null -eq $app) {
        $app = New-AzureADApplication -Oauth2AllowImplicitFlow $true -RequiredResourceAccess $permission -DisplayName $appName -ReplyUrls $replyUrls.name -AvailableToOtherTenants $isMultiTenant -GroupMembershipClaims $groupMembershipClaims
    }
    else {
        Set-AzureADApplication -ObjectId $app.ObjectId -RequiredResourceAccess $permission -Oauth2AllowImplicitFlow $true -ReplyUrls $replyUrls.name -AvailableToOtherTenants $isMultiTenant -GroupMembershipClaims $groupMembershipClaims
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
    $completed = $false
    [int]$retryCount = 0

    foreach($key in $filteredKeys) {
        Write-Host "Removing existing password credential named `"$($encoding.GetString($key.CustomKeyIdentifier))`" with id $($key.KeyId)"
        do {
            if($retryCount -gt 3) {
                Write-DosMessage -Level "Error" -Message "Could not create Azure AD application secret."
                throw
            }

            try {
                Remove-AzureADApplicationPasswordCredential -ObjectId $objectId -KeyId $key.KeyId -ErrorAction 'stop'
                $completed = $true
            }
            catch {
                Write-DosMessage -Level "Warning" -Message "An error occurred trying to remove the Azure application secret named `"$($encoding.GetString($key.CustomKeyIdentifier))`" with id $($key.KeyId). Retrying.."
                Start-Sleep 3
                $retryCount++
            }
        } while ($completed -eq $false)
    }
}

function Get-FabricAzureADSecret([string] $objectId) {
    # Cleanup existing secret
    $keyCredentialName = "PowerShell Created Password"
    Remove-AzureADClientSecret -objectId $objectId -keyIdentifier $keyCredentialName
    Write-Host "Creating password credential named $keyCredentialName"
    $completed = $false
    [int]$retryCount = 0

    do{
        if($retryCount -gt 3) {
            Write-DosMessage -Level "Error" -Message "Could not create Azure AD application secret."
            throw
        }

        try {
            $credential = New-AzureADApplicationPasswordCredential -ObjectId $objectId -CustomKeyIdentifier $keyCredentialName -ErrorAction 'stop'
            $completed = $true
        }
        catch {
            Write-DosMessage -Level "Warning" -Message "An error occurred trying to create the Azure application secret named $keyCredentialName. Retrying.."
            Start-Sleep 3
            $retryCount++
        }
    } While ($completed -eq $false)
    return $credential.Value
}

function Connect-AzureADTenant {
    param(
        [Parameter(Mandatory=$true)]
        [string] $tenantId,
        [PSCredential] $credential
    )

    try {
        if($credential) {
            Connect-AzureAD -Credential $credential -TenantId $tenantId | Out-Null
        } else {
            Connect-AzureAD -TenantId $tenantId | Out-Null
        }
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
    $tenants += Get-TenantSettingsFromInstallConfig -installConfigPath $installConfigPath `
        -scope $scope `
        -setting $parentSetting

    if($null -eq $tenants -or $tenants.Count -eq 0){
        Write-DosMessage -Level "Error" -Message  "No tenants to register were found in the install.config"
        throw
    }
    Confirm-Tenants $tenants

    return $tenants
}

function Get-ReplyUrls {
    param(
        [string] $installConfigPath
    )
    $scope = "identity"
    $parentSetting = "replyUrls"
    $replyUrls = @()
    $replyUrls += Get-TenantSettingsFromInstallConfig -installConfigPath $installConfigPath -scope $scope -setting $parentSetting

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
        [HashTable[]] $replyUrls,
        [Parameter(Mandatory=$true)]
        [string] $configSection,
        [Parameter(Mandatory=$true)]
        [string] $installConfigPath
    )
    $allowedTenantsText = "allowedTenants"
    $claimsIssuerText = "claimsIssuerTenant"
    $allowedTenants += Get-TenantSettingsFromInstallConfig -installConfigPath $installConfigPath `
        -scope $configSection `
        -setting $allowedTenantsText

    $claimsIssuer += Get-TenantSettingsFromInstallConfig -installConfigPath $installConfigPath `
        -scope $configSection `
        -setting $claimsIssuerText
    Confirm-Tenants -tenants $allowedTenants
    Confirm-Tenants -tenants $claimsIssuer

   if($null -ne $claimsIssuer.name) {
    Write-Host "Enter credentials for $appName specified tenant: $($claimsIssuer.name)"
    Connect-AzureADTenant -tenantId $claimsIssuer.name

    $permission = Get-GraphApiUserReadPermissions
    $app = New-FabricAzureADApplication -appName $appName -replyUrls $replyUrls -permission $permission -isMultiTenant $true
    $clientId = $app.AppId
    $clientSecret = Get-FabricAzureADSecret -objectId $app.ObjectId

    Disconnect-AzureAD

    Add-InstallationTenantSettings -configSection $configSection `
    -tenantId $claimsIssuer.name `
    -tenantAlias $claimsIssuer.alias `
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

function Register-IdPSS {
    param (
        [Parameter(Mandatory=$true)]
        [string] $appName,
        [Parameter(Mandatory=$true)]
        [HashTable[]] $replyUrls,
        [Parameter(Mandatory=$true)]
        [HashTable[]] $tenants,
        [Parameter(Mandatory=$true)]
        [string] $configSection,
        [Parameter(Mandatory=$true)]
        [string] $installConfigPath
    )
    # IdentityProviderSearchService registration
   if($null -ne $tenants) {
    foreach($tenant in $tenants) { 
      Write-Host "Enter credentials for $appName on tenant specified: $($tenant.name)"
      Connect-AzureADTenant -tenantId $tenant.name

      # Get read permissions
      $permission = Get-GraphApiDirectoryReadPermissions
      $app = New-FabricAzureADApplication -appName $appName -replyUrls $replyUrls -permission $permission
      $clientId = $app.AppId
      $clientSecret = Get-FabricAzureADSecret -objectId $app.ObjectId

      Disconnect-AzureAD
      Add-InstallationTenantSettings -configSection $configSection `
          -tenantId $tenant.name `
          -tenantAlias $tenant.alias `
          -clientSecret $clientSecret `
          -clientId $clientId `
          -installConfigPath $installConfigPath `
          -appName $appName

      # Manual process, need to give consent this way for now
      Start-Process -FilePath  "https://login.microsoftonline.com/$($tenant.name)/oauth2/authorize?client_id=$clientId&response_type=code&state=12345&prompt=admin_consent"
    }
 }
}

function Confirm-Tenants {
    param(
        [Parameter(Mandatory=$true)]
        [Object[]] $tenants
    )
    foreach($tenant in $tenants) {
        if([string]::IsNullOrEmpty($tenant.name) -or [string]::IsNullOrEmpty($tenant.alias)) {
            Write-DosMessage -Level "Fatal" -Message "Tenant alias and name must be provided for each tenant."
        }
    }
}

Export-ModuleMember Get-FabricAzureADSecret
Export-ModuleMember Connect-AzureADTenant
Export-ModuleMember New-FabricAzureADApplication
Export-ModuleMember Get-Tenants
Export-ModuleMember Get-ReplyUrls
Export-ModuleMember Register-Identity
Export-ModuleMember Register-IdPSS