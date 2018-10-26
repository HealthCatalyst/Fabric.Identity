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
    $appName = "PowerShell Registration Application"
    # Get read permissions
    $readResource = Get-GraphApiReadPermissions

    # TODO: Pull out name into a config ?
    $app = Get-AzureADApplication -Filter "DisplayName eq '$appName'" -Top 1
        # TODO: if does not exist, create, else update
    if($null -eq $app) {
        # create new application
        # TODO: Get name from config?
        # TODO: Add redirect url
        $app = New-AzureADApplication -Oauth2AllowImplicitFlow $true -RequiredResourceAccess $readResource -DisplayName $appName
    }
    else {
        # TODO: Add redirect url
        Set-AzureADApplication -ObjectId $app.ObjectId -RequiredResourceAccess $readWriteAccess -Oauth2AllowImplicitFlow $true 
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

Export-ModuleMember Get-FabricAzureADSecret
Export-ModuleMember Connect-AzureADTenant
Export-ModuleMember New-FabricAzureADApplication