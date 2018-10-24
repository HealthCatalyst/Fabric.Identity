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
    # Get read permissions
    $readResource = Get-GraphApiReadPermissions

    # TODO: Pull out name into a config ?
    $app = Get-AzureADApplication -Filter "DisplayName eq 'PowerShell Registration Application'" -Top 1
        # TODO: if does not exist, create, else update
    if($null -eq $app) {
        # create new application
        # TODO: Get name from config?
        # TODO: Add redirect url
        $app = New-AzureADApplication -Oauth2AllowImplicitFlow $true -RequiredResourceAccess $readResource -DisplayName "PowerShell Registration Application"
    }
    else {
        # TODO: Add redirect url
        $app = Set-AzureADApplication -ObjectId $app.ObjectId -RequiredResourceAccess $readWriteAccess -Oauth2AllowImplicitFlow $true 
    }

    return $app
    # TODO: Store the $app.AppId as the client-id
}

function Get-FabricAzureADSecret([string] $objectId) {
    # TODO: Remove and recreate on every install?
    $credential = Get-AzureADApplicationPasswordCredential -ObjectId $objectId
    if($null -eq $app) {
        $credential = New-AzureADApplicationPasswordCredential -ObjectId $objectId
    }

    return $credential
}


function New-FabricAzureADApplicationRoot([string] $tenantId, [PSCredential] $credentials) {
    # Step 1. Connect to azure tenant
    Connect-AzureAD -Credential $credential -TenantId $tenantId

    # Step 2. Create or update an application    
    $app = New-FabricAzureADApplication
    
    #Step 3. Get Client secret
    # TODO: Store client secret
    $clientSecret = Get-FabricAzureADSecret -objectId $app.ObjectId

    #Step 4.
    # Give admin consent to application?
    # Can we automate this?
    # Start-Process -FilePath  "https://login.microsoftonline.com/4d07d6d8-58e4-45a4-8ce9-5d2cfc00c65f/oauth2/authorize?client_id=e4fd028e-51ac-4c69-aee6-de0519566f5b&response_type=code&state=12345&prompt=admin_consent"
    # Kind of a manual process, but we can put up a window here during an interactive install


    # Step 5. Disconnect
    Disconnect-AzureAD
}


# Is there a way to specify the tenant you want to connect to? Array in config settings and loop through? Which is the main app to register to also be identity?
$tenantId = "" # TODO: Get Tenant ID, from config possibly?
$credentials = Get-Credential # TODO: not automated, it's unlikely we'll have passwords in a config file either, but can pass in as a script parameter

New-FabricAzureADApplicationRoot -tenantId $tenantId -credentials $credentials