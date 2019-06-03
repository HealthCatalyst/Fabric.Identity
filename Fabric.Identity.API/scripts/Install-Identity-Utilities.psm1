# Import Dos Install Utilities
$minVersion = [System.Version]::new(1, 0, 248 , 0)
try {
    Get-InstalledModule -Name DosInstallUtilities -MinimumVersion $minVersion -ErrorAction Stop
} catch {
    Write-Host "Installing DosInstallUtilities from Powershell Gallery"
    Install-Module DosInstallUtilities -Scope CurrentUser -MinimumVersion $minVersion -Force
}
Import-Module -Name DosInstallUtilities -MinimumVersion $minVersion -Force

# Import Fabric Install Utilities
$fabricInstallUtilities = ".\Fabric-Install-Utilities.psm1"
if (!(Test-Path $fabricInstallUtilities -PathType Leaf)) {
    Write-DosMessage -Level "Warning" -Message "Could not find fabric install utilities. Manually downloading and installing"
    Get-WebRequestDownload -Uri https://raw.githubusercontent.com/HealthCatalyst/InstallScripts/master/common/Fabric-Install-Utilities.psm1 -NoCache -OutFile $fabricInstallUtilities
}
Import-Module -Name $fabricInstallUtilities -Force

# Import CatalystDosIdentity
$minDosIdentityVersion = [System.Version]::new(1, 4, 18200 , 12)
try{
    Get-InstalledModule -Name CatalystDosIdentity -MinimumVersion $minDosIdentityVersion -ErrorAction Stop
} catch{
    Write-Host "Installing CatalystDosIdentity from Powershell Gallery"
    Install-Module CatalystDosIdentity -Scope CurrentUser -MinimumVersion $minDosIdentityVersion -Force
}
Import-Module -Name CatalystDosIdentity -MinimumVersion $minVersion -Force

$Global:idPSSAppName = "Identity Provider Search Service"

function Get-FullyQualifiedInstallationZipFile([string] $zipPackage, [string] $workingDirectory){
    if((Test-Path $zipPackage))
    {
        $path = [System.IO.Path]::GetDirectoryName($zipPackage)
        if(!$path)
        {
            $zipPackage = [System.IO.Path]::Combine($workingDirectory, $zipPackage)
        }
        Write-DosMessage -Level "Information" -Message "ZipPackage: $zipPackage is present."
        return $zipPackage
    }else{
        Write-DosMessage -Level "Error" -Message "Could not find file or directory $zipPackage, please verify that the zipPackage configuration setting in install.config is the path to a valid zip file that exists."
        throw
    }
}

function Install-DotNetCoreIfNeeded([string] $version, [string] $downloadUrl){
    if(!(Test-PrerequisiteExact "*.NET Core*Windows Server Hosting*" $version))
    {    
        try{
            Write-DosMessage -Level "Information" -Message "Windows Server Hosting Bundle version $version not installed...installing version $version"        
            Get-WebRequestDownload -Uri $downloadUrl -OutFile $env:Temp\bundle.exe
            Start-Process $env:Temp\bundle.exe -Wait -ArgumentList '/quiet /install'
            Restart-W3SVC
        }catch{
            Write-DosMessage -Level "Error" -Message "Could not install .NET Windows Server Hosting bundle. Please install the hosting bundle before proceeding. $downloadUrl"
            throw
        }
        try{
            Remove-Item $env:Temp\bundle.exe
        }catch{
            $e = $_.Exception
            Write-DosMessage -Level "Warning" -Message "Unable to remove temporary download file for server hosting bundle exe" 
            Write-DosMessage -Level "Warning" -Message  $e.Message
        }

    }else{
        Write-DosMessage -Level "Information" -Message  ".NET Core Windows Server Hosting Bundle (v$version) installed and meets expectations."
    }
}

function Get-IISWebSiteForInstall([string] $selectedSiteName, [string] $installConfigPath, [string] $scope, [bool] $quiet){
    try{
        $sites = Get-ChildItem IIS:\Sites
        if($quiet -eq $true){
            $selectedSite = $sites | Where-Object { $_.Name -eq $selectedSiteName }
        }else{
            if($sites -is [array]){
                $sites |
                    ForEach-Object {New-Object PSCustomObject -Property @{
                        'Id'=$_.id;
                        'Name'=$_.name;
                        'Physical Path'=[System.Environment]::ExpandEnvironmentVariables($_.physicalPath);
                        'Bindings'=$_.bindings;
                    };} |
                    Format-Table Id,Name,'Physical Path',Bindings -AutoSize | Out-Host
                
                $attempts = 1
                do {
                    if($attempts -gt 10){
                        Write-DosMessage -Level "Error" -Message "An invalid website has been selected."
                        throw
                    }
                    $selectedSiteId = Read-Host "Select a web site by Id"
                    $selectedSite = $sites[$selectedSiteId - 1]
                    if([string]::IsNullOrEmpty($selectedSiteId)){
                        Write-DosMessage -Level "Information" -Message "You must select a web site."
                    }
                    if($null -eq $selectedSite){
                        Write-DosMessage -Level "Information" -Message "You must select a web site by id between 1 and $($sites.Count)."
                    }
                    $attempts++
                } while ([string]::IsNullOrEmpty($selectedSiteId) -or ($null -eq $selectedSite))
                
            }else{
                $selectedSite = $sites
            }
        }
        if($null -eq $selectedSite){
            throw "Could not find selected site."
        }
        if($selectedSite.Name){ Add-InstallationSetting "$scope" "siteName" $selectedSite.Name $installConfigPath | Out-Null }

        return $selectedSite

    }catch{
        Write-DosMessage -Level "Error" -Message "Could not select a website."
        throw
    }
}

function New-SigningAndEncryptionCertificate([string] $subject, [string] $certStorelocation)
{
    $cert = New-SelfSignedCertificate -Type Custom -Subject $subject -KeyUsage DataEncipherment, DigitalSignature -KeyAlgorithm RSA -KeyLength 2048 -CertStoreLocation $certStoreLocation
    return $cert
}

function Get-Certificates([string] $primarySigningCertificateThumbprint, [string] $encryptionCertificateThumbprint, [string] $installConfigPath, [string] $scope, [bool] $quiet){
    if(Test-ShouldShowCertMenu -primarySigningCertificateThumbprint $primarySigningCertificateThumbprint `
                                -encryptionCertificateThumbprint $encryptionCertificateThumbprint `
                                -quiet $quiet){
        try{
            $today = Get-Date
            $allCerts = Get-CertsFromLocation Cert:\LocalMachine\My
            $index = 1
            $attempts = 1
            $allCerts | 
                Where-Object { $_.NotAfter -ge $today -and $_.NotBefore -le $today } |
                ForEach-Object {New-Object PSCustomObject -Property @{
                'Index'=$index;
                'Subject'= $_.Subject; 
                'Name' = $_.FriendlyName; 
                'Thumbprint' = $_.Thumbprint; 
                'Expiration' = $_.NotAfter
                };
                $index ++} |
                Format-Table Index, Thumbprint, Name, Subject, Expiration -AutoSize | Out-Host
            do {
                if($attempts -gt 10){
                    Write-DosMessage -Level "Error" -Message "An invalid certificate has been selected."
                    throw
                }
                $selectionNumber = Read-Host  "Select a signing and encryption certificate by Index"
                if([string]::IsNullOrEmpty($selectionNumber)){
                    Write-DosMessage -Level "Information" -Message "You must select a certificate so Fabric.Identity can sign access and identity tokens."
                }else{
                    $selectionNumberAsInt = [convert]::ToInt32($selectionNumber, 10)
                    if(($selectionNumberAsInt -gt  $allCerts.Count) -or ($selectionNumberAsInt -le 0)){
                        Write-DosMessage -Level "Information" -Message  "Please select a certificate with index between 1 and $($allCerts.Count)."
                    }
                }
                $attempts++
            } while ([string]::IsNullOrEmpty($selectionNumber) -or ($selectionNumberAsInt -gt $allCerts.Count) -or ($selectionNumberAsInt -le 0))

            $certThumbprint = Get-CertThumbprint $allCerts $selectionNumberAsInt
            
            if([string]::IsNullOrWhitespace($primarySigningCertificateThumbprint)){
                $primarySigningCertificateThumbprint = $certThumbprint -replace '[^a-zA-Z0-9]', ''
            }
    
            if ([string]::IsNullOrWhitespace($encryptionCertificateThumbprint)){
                $encryptionCertificateThumbprint = $certThumbprint -replace '[^a-zA-Z0-9]', ''
            }
    
        }catch{
            Write-DosMessage -Level "Error" -Message  "Could not set the certificate thumbprint. Error $($_.Exception.Message)"
            throw
        }
    }
    try{
        $signingCert = Get-Certificate ($primarySigningCertificateThumbprint -replace '[^a-zA-Z0-9]', '')
    }catch{
        Write-DosMessage -Level "Error" -Message  "Could not get signing certificate with thumbprint $primarySigningCertificateThumbprint. Please verify that the primarySigningCertificateThumbprint setting in install.config contains a valid thumbprint for a certificate in the Local Machine Personal store."
        throw $_.Exception
    }

    try{
        $encryptionCert = Get-Certificate ($encryptionCertificateThumbprint -replace '[^a-zA-Z0-9]', '')
    }catch{
        Write-DosMessage -Level "Error" -Message  "Could not get encryption certificate with thumbprint $encryptionCertificateThumbprint. Please verify that the encryptionCertificateThumbprint setting in install.config contains a valid thumbprint for a certificate in the Local Machine Personal store."
        throw $_.Exception
    }
    if($encryptionCert.Thumbprint){ Add-InstallationSetting "common" "encryptionCertificateThumbprint" $encryptionCert.Thumbprint $installConfigPath | Out-Null }
    if($encryptionCert.Thumbprint){ Add-InstallationSetting "$scope" "encryptionCertificateThumbprint" $encryptionCert.Thumbprint $installConfigPath | Out-Null }
    if($signingCert.Thumbprint){ Add-InstallationSetting "$scope" "primarySigningCertificateThumbprint" $signingCert.Thumbprint $installConfigPath | Out-Null }
    return @{SigningCertificate = $signingCert; EncryptionCertificate = $encryptionCert}
}

function Get-IISAppPoolUser([PSCredential] $credential, [string] $appName, [string] $storedIisUser, [string] $installConfigPath, [string] $scope){
    if($credential){
        Confirm-Credentials -credential $credential
        $iisUser = "$($credential.GetNetworkCredential().Domain)\$($credential.GetNetworkCredential().UserName)"
    }
    elseif(Test-AppPoolExistsAndRunsAsUser -appPoolName $appName -userName $storedIisUser){
        $iisUser = $storedIisUser
    }
    else{
        if(![string]::IsNullOrEmpty($storedIisUser)){
            $userEnteredIisUser = Read-Host "Press Enter to accept the default IIS App Pool User '$($storedIisUser)' or enter a new App Pool User"
            if([string]::IsNullOrEmpty($userEnteredIisUser)){
                $userEnteredIisUser = $storedIisUser
            }
        }else{
            $userEnteredIisUser = Read-Host "Please enter a user account for the App Pool"
        }
    
        if(![string]::IsNullOrEmpty($userEnteredIisUser)){
        
            $iisUser = $userEnteredIisUser
            $userEnteredPassword = Read-Host "Enter the password for $iisUser" -AsSecureString
            $credential = Get-ConfirmedCredentials -iisUser $iisUser -userEnteredPassword $userEnteredPassword
            Write-DosMessage -Level "Information" -Message "Credentials are valid for user $iisUser"
        }else{
            Write-DosMessage -Level "Error" -Message "No user account was entered, please enter a valid user account."
            throw
        }
    }
    if($iisUser){ Add-InstallationSetting "$scope" "iisUser" "$iisUser" $installConfigPath | Out-Null }
    return @{UserName = $iisUser; Credential = $credential}
}

function Get-ConfirmedCredentials([string] $iisUser, [SecureString] $userEnteredPassword){
    $credential = New-Object -TypeName "System.Management.Automation.PSCredential" -ArgumentList $iisUser, $userEnteredPassword
    Confirm-Credentials -credential $credential
    return $credential
}

function Confirm-Credentials([PSCredential] $credential){
    [System.Reflection.Assembly]::LoadWithPartialName("System.DirectoryServices.AccountManagement") | Out-Null
    $ct = [System.DirectoryServices.AccountManagement.ContextType]::Domain
    $pc = New-Object System.DirectoryServices.AccountManagement.PrincipalContext -ArgumentList $ct,$credential.GetNetworkCredential().Domain
    Write-Host "Confirming credentials"
    $isValid = $pc.ValidateCredentials($credential.GetNetworkCredential().UserName, $credential.GetNetworkCredential().Password, [System.DirectoryServices.AccountManagement.ContextOptions]::Negotiate)
    if(!$isValid){
        Write-DosMessage -Level "Error" -Message "Incorrect credentials for $($credential.GetNetworkCredential().UserName)"
        throw
    }
}

function Add-PermissionToPrivateKey([string] $iisUser, [System.Security.Cryptography.X509Certificates.X509Certificate2] $signingCert, [string] $permission){
    try{
        $allowRule = New-Object security.accesscontrol.filesystemaccessrule $iisUser, $permission, allow
        $keyFolder = "c:\programdata\microsoft\crypto\rsa\machinekeys"

        $keyname = $signingCert.privatekey.cspkeycontainerinfo.uniquekeycontainername
        $keyPath = [io.path]::combine($keyFolder, $keyname)

        if ([io.file]::exists($keyPath))
        {        
            $acl = Get-Acl $keyPath
            $acl.AddAccessRule($allowRule)
            Set-Acl $keyPath $acl -ErrorAction Stop
            Write-DosMessage -Level "Information" -Message "The permission '$($permission)' was successfully added to the private key for user '$($iisUser)'"
        }else{
            Write-DosMessage -Level "Error" -Message "No key file was found at '$($keyPath)' for '$($signingCert)'. Ensure a valid signing certificate was provided"
            throw
        }
    }catch{
        Write-DosMessage -Level "Error" -Message "There was an error adding the '$($permission)' permission for the user '$($iisUser)' to the private key. Ensure you selected a certificate that you have read access on the private key. Error $($_.Exception.Message)."
        throw
    }
}

function Get-AppInsightsKey([string] $appInsightsInstrumentationKey, [string] $installConfigPath, [string] $scope, [bool] $quiet){
    if(!$quiet){
        $userEnteredAppInsightsInstrumentationKey = Read-Host  "Enter Application Insights instrumentation key or hit enter to accept the default [$appInsightsInstrumentationKey]"

        if(![string]::IsNullOrEmpty($userEnteredAppInsightsInstrumentationKey)){   
            $appInsightsInstrumentationKey = $userEnteredAppInsightsInstrumentationKey
        }
    }
    if($appInsightsInstrumentationKey){ Add-InstallationSetting "$scope" "appInsightsInstrumentationKey" "$appInsightsInstrumentationKey" $installConfigPath | Out-Null }
    if($appInsightsInstrumentationKey){ Add-InstallationSetting "common" "appInsightsInstrumentationKey" "$appInsightsInstrumentationKey" $installConfigPath | Out-Null }
    return $appInsightsInstrumentationKey
}

function Get-SqlServerAddress([string] $sqlServerAddress, [string] $installConfigPath, [bool] $quiet){
    if(!$quiet){
        $userEnteredSqlServerAddress = Read-Host "Press Enter to accept the default Sql Server address '$($sqlServerAddress)' or enter a new Sql Server address" 

        if(![string]::IsNullOrEmpty($userEnteredSqlServerAddress)){
            $sqlServerAddress = $userEnteredSqlServerAddress
        }
    }
    if($sqlServerAddress){ Add-InstallationSetting "common" "sqlServerAddress" "$sqlServerAddress" $installConfigPath | Out-Null }
    return $sqlServerAddress
}

function Get-IdentityDatabaseConnectionString([string] $identityDbName, [string] $sqlServerAddress, [string] $installConfigPath, [bool] $quiet){
    if(!$quiet){
        $userEnteredIdentityDbName = Read-Host "Press Enter to accept the default Identity DB Name '$($identityDbName)' or enter a new Identity DB Name"
        if(![string]::IsNullOrEmpty($userEnteredIdentityDbName)){
            $identityDbName = $userEnteredIdentityDbName
        }
    }
    $identityDbConnStr = "Server=$($sqlServerAddress);Database=$($identityDbName);Trusted_Connection=True;MultipleActiveResultSets=True;"

    try {
        Invoke-Sql $identityDbConnStr "SELECT TOP 1 ClientId FROM Clients" | Out-Null
        Write-DosMessage -Level "Information" -Message "Identity DB Connection string: $identityDbConnStr verified"
    }
    catch {
        Write-DosMessage -Level "Fatal" -Message "An error occurred while executing the command. Connection String: $($identityDbConnStr). Error $($_.Exception)"
    }
    if($identityDbName){ Add-InstallationSetting "identity" "identityDbName" "$identityDbName" $installConfigPath | Out-Null }
    return @{DbName = $identityDbName; DbConnectionString = $identityDbConnStr}
}

function Get-MetadataDatabaseConnectionString([string] $metadataDbName, [string] $sqlServerAddress, [string] $installConfigPath, [bool] $quiet){
    if(!($quiet)){
        $userEnteredMetadataDbName = Read-Host "Press Enter to accept the default Metadata DB Name '$($metadataDbName)' or enter a new Metadata DB Name"
        if(![string]::IsNullOrEmpty($userEnteredMetadataDbName)){
            $metadataDbName = $userEnteredMetadataDbName
        }
    }
    $metadataConnStr = "Server=$($sqlServerAddress);Database=$($metadataDbName);Trusted_Connection=True;MultipleActiveResultSets=True;"

    try {
        Invoke-Sql $metadataConnStr "SELECT TOP 1 RoleID FROM CatalystAdmin.RoleBASE" | Out-Null
        Write-DosMessage -Level "Information" -Message "Metadata DB Connection string: $metadataConnStr verified"
    }
    catch {
        Write-DosMessage -Level "Fatal" -Message "An error occurred while executing the command. Connection String: $($metadataConnStr). Error $($_.Exception)"
    }
    if($metadataDbName){ Add-InstallationSetting "common" "metadataDbName" "$metadataDbName" $installConfigPath | Out-Null }
    return @{DbName = $metadataDbName; DbConnectionString = $metadataConnStr}
}

function Get-MetadataConnectionStringForDiscovery{
    param(
        [Parameter(Mandatory=$true)]
        [Hashtable] $commonConfig
    )
    
    $metaDataConnectionString =  "Data Source=$($commonConfig.sqlServerAddress);Initial Catalog=$($commonConfig.metadataDbName);Integrated Security=True;Application Name=Discovery Service;"
    Confirm-DatabaseConnection -connectionString $metaDataConnectionString
    return $metaDataConnectionString
}


function Confirm-DatabaseConnection{
    param(
        [Parameter(Mandatory=$true)]
        [string] $connectionString
    )

    Write-DosMessage -Level "Information" -Message "Confirming connection string '$connectionString'."
    $connection = New-Object System.Data.SqlClient.SQLConnection($connectionString)
    try{
        $connection.Open()
    }catch{
        Write-DosMessage -Level "Fatal" -Message "Could not connect to '$connectionString' please check database connection settings in install config."
    }finally{
        $connection.Close();
    }
}


function Get-DiscoveryServiceUrl([string]$discoveryServiceUrl, [string] $installConfigPath, [bool]$quiet){
    $defaultDiscoUrl = Get-DefaultDiscoveryServiceUrl -discoUrl $discoveryServiceUrl
    if(!$quiet){
        $userEnteredDiscoveryServiceUrl = Read-Host "Press Enter to accept the default DiscoveryService URL [$defaultDiscoUrl] or enter a new URL"
        if(![string]::IsNullOrEmpty($userEnteredDiscoveryServiceUrl)){   
            $defaultDiscoUrl = $userEnteredDiscoveryServiceUrl
        }
    }
    if($defaultDiscoUrl){ Add-InstallationSetting "common" "discoveryService" "$defaultDiscoUrl" $installConfigPath | Out-Null }
    return $defaultDiscoUrl
}

function Get-ApplicationEndpoint([string] $appName, [string] $applicationEndpoint, [string] $installConfigPath, [string] $scope, [bool] $quiet){
    $defaultAppEndpoint = Get-DefaultApplicationEndpoint -appName $appName -appEndPoint $applicationEndpoint
    if(!$quiet){
        $userEnteredApplicationEndpoint = Read-Host "Press Enter to accept the default Application Endpoint URL [$defaultAppEndpoint] or enter a new URL"
        if(![string]::IsNullOrEmpty($userEnteredApplicationEndpoint)){
            $defaultAppEndpoint = $userEnteredApplicationEndpoint
        }
    }
    if($defaultAppEndpoint){ Add-InstallationSetting $scope "applicationEndPoint" "$defaultAppEndpoint" $installConfigPath | Out-Null }
    if($defaultAppEndpoint){ Add-InstallationSetting "common" "$($scope)Service" "$defaultAppEndpoint" $installConfigPath | Out-Null }
    return $defaultAppEndpoint
}

function Unlock-ConfigurationSections(){   
    [System.Reflection.Assembly]::LoadFrom("$env:systemroot\system32\inetsrv\Microsoft.Web.Administration.dll") | Out-Null
    $manager = new-object Microsoft.Web.Administration.ServerManager      
    $config = $manager.GetApplicationHostConfiguration()

    $section = $config.GetSection("system.webServer/security/authentication/anonymousAuthentication")
    $section.OverrideMode = "Allow"    
    Write-DosMessage -Level "Information" -Message "Unlocked system.webServer/security/authentication/anonymousAuthentication"

    $section = $config.GetSection("system.webServer/security/authentication/windowsAuthentication")
    $section.OverrideMode = "Allow"    
    Write-DosMessage -Level "Information" -Message "Unlocked system.webServer/security/authentication/windowsAuthentication"
    
    $manager.CommitChanges()
}

function Publish-Application([System.Object] $site, [string] $appName, [hashtable] $iisUser, [string] $zipPackage, [string] $assembly){
    $appDirectory = [io.path]::combine([System.Environment]::ExpandEnvironmentVariables($site.physicalPath), $appName)
    New-LogsDirectoryForApp $appDirectory $iisUser.UserName

    if(!(Test-AppPoolExistsAndRunsAsUser -appPoolName $appName -userName $iisUser.UserName)){
        New-AppPool $appName $iisUser.UserName $iisUser.Credential
    }

    New-App $appName $site.Name $appDirectory | Out-Null
    Publish-WebSite $zipPackage $appDirectory $appName $true
    Set-Location $PSScriptRoot
    $version = Get-InstalledVersion -appDirectory $appDirectory -assemblyPath $assembly
    return @{applicationDirectory = $appDirectory; version = $version }
}

function Get-InstalledVersion([string] $appDirectory, [string] $assemblyPath){
    return [System.Diagnostics.FileVersionInfo]::GetVersionInfo("$appDirectory\$assemblyPath").FileVersion
}

function Register-ServiceWithDiscovery
(
  [string] $iisUserName, 
  [string] $metadataConnStr, 
  [string] $version, 
  [string] $serverUrl, 
  [string] $serviceName, 
  [string] $friendlyName, 
  [string] $description
)
{
    Add-ServiceUserToDiscovery $iisUserName $metadataConnStr

    $discoveryPostBody = @{
        buildVersion = $version;
        serviceName = $serviceName;
        serviceVersion = 1;
        friendlyName = $friendlyName;
        description = $description;
        serverUrl = $serverUrl;
        serviceUrl = $serverUrl;
        discoveryType = "Service";
    }
    Add-DiscoveryRegistrationSql -discoveryPostBody $discoveryPostBody -connectionString $metadataConnStr | Out-Null
    Write-DosMessage -Level "Information" -Message "$serviceName registered URL: $serverUrl with DiscoveryService."
}

function Add-DatabaseSecurity([string] $userName, [string] $role, [string] $connString)
{
    Add-DatabaseLogin $userName $connString
    Add-DatabaseUser $userName $connString
    Add-DatabaseUserToRole $userName $connString $role
    Write-DosMessage -Level "Information" -Message "Database security applied successfully"
}

function Set-IdentityEnvironmentVariables([string] $appDirectory, `
    [string] $primarySigningCertificateThumbprint, `
    [string] $encryptionCertificateThumbprint, `
    [string] $appInsightsInstrumentationKey, `
    [string] $applicationEndpoint, `
    [string] $identityDbConnStr, `
    [string] $discoveryServiceUrl, `
    [bool] $noDiscoveryService){
    $environmentVariables = @{"HostingOptions__StorageProvider" = "SqlServer"; "HostingOptions__UseTestUsers" = "false"; "AllowLocalLogin" = "false"}

    if ($primarySigningCertificateThumbprint){
        $environmentVariables.Add("SigningCertificateSettings__UseTemporarySigningCredential", "false")
        $environmentVariables.Add("SigningCertificateSettings__PrimaryCertificateThumbprint", $primarySigningCertificateThumbprint)
    }

    if ($encryptionCertificateThumbprint){
        $environmentVariables.Add("SigningCertificateSettings__EncryptionCertificateThumbprint", $encryptionCertificateThumbprint)
    }

    if($appInsightsInstrumentationKey){
        $environmentVariables.Add("ApplicationInsights__Enabled", "true")
        $environmentVariables.Add("ApplicationInsights__InstrumentationKey", $appInsightsInstrumentationKey)
    }

    $environmentVariables.Add("IdentityServerConfidentialClientSettings__Authority", "$applicationEndpoint")

    if($identityDbConnStr){
        $environmentVariables.Add("ConnectionStrings__IdentityDatabase", $identityDbConnStr)
    }

    if(!($noDiscoveryService) -and $discoveryServiceUrl){
        $environmentVariables.Add("DiscoveryServiceEndpoint", "$discoveryServiceUrl")
        $environmentVariables.Add("UseDiscoveryService", "true")
    }else{
        $environmentVariables.Add("UseDiscoveryService", "false")
    }

    Set-EnvironmentVariables $appDirectory $environmentVariables | Out-Null
}

function Add-RegistrationApiRegistration([string] $identityServerUrl, [string] $accessToken){
    $body = @{
        Name = "registration-api";
        UserClaims = @("name","email","role","groups");
        Scopes = @(@{Name = "fabric/identity.manageresources"}, @{ Name = "fabric/identity.read"}, @{ Name = "fabric/identity.searchusers"});
    }
    $jsonBody = ConvertTo-Json $body

    Write-DosMessage -Level "Information" -Message "Registering Fabric.Identity Registration API."
    $registrationApiSecret = ([string](Add-ApiRegistration -authUrl $identityServerUrl -body $jsonBody -accessToken $accessToken)).Trim()
    return $registrationApiSecret
}

function Add-IdpssApiResourceRegistration($identityServiceUrl, $fabricInstallerSecret)
{
        $accessToken = Get-AccessToken -identityUrl $identityServiceUrl -clientId "fabric-installer" -secret $fabricInstallerSecret -scope "fabric/identity.manageresources"

    Write-DosMessage -Level "Information" -Message "Registering IdentitySearchProvider API with Fabric.Identity..."
    $apiName = "idpsearch-api"
    try{
        [string]$apiSecret = [string]::Empty
        Write-Host "    Registering $($apiName) with Fabric.Identity"
        $body = New-APIRegistrationBody -apiName $apiName -userClaims @("name", "email", "roles", "group") -scopes @{"name" = "fabric/idprovider.searchusers"} -isEnabled $true
        $apiSecret = New-ApiRegistration -identityUrl $identityServiceUrl -body (ConvertTo-Json $body) -accessToken $accessToken

        if (![string]::IsNullOrWhiteSpace($apiSecret)) {
          return $apiSecret
        }
        else
        {
          Write-DosMessage -Level "Error" -Message "Could not register api $($apiName), apiSecret is empty"
        }
    }
    catch{
        Write-DosMessage -Level "Error" -Message "Could not register api $($apiName)"
        throw $_.Exception
    }
}


function Add-InstallerClientRegistration([string] $identityServerUrl, [string] $accessToken, [string] $fabricInstallerSecret){
    $body = @{
        ClientId = "fabric-installer";
        ClientName = "Fabric Installer";
        RequireConsent = $false;
        AllowedGrantTypes = @("client_credentials");
        AllowedScopes = @("fabric/identity.manageresources", "fabric/authorization.read", "fabric/authorization.write", "fabric/authorization.dos.write", "fabric/authorization.manageclients", "dos/metadata", "dos/metadata.serviceAdmin")
    }
    $jsonBody = ConvertTo-Json $body

    Write-DosMessage -Level "Information" -Message "Registering Fabric.Installer Client."
    $installerClientSecret = ([string](Add-ClientRegistration -authUrl $identityServerUrl -body $jsonBody -accessToken $accessToken -shouldResetSecret $false)).Trim()
    
    if([string]::IsNullOrWhiteSpace($installerClientSecret)) {
        $installerClientSecret = $fabricInstallerSecret
    }
    return $installerClientSecret
}

function Add-IdentityClientRegistration([string] $identityServerUrl, [string] $accessToken){
    $body = @{
        ClientId = "fabric-identity-client"; 
        ClientName = "Fabric Identity Client"; 
        RequireConsent = $false;
        AllowedGrantTypes = @("client_credentials"); 
        AllowedScopes = @("fabric/idprovider.searchusers");
    }
    $jsonBody = ConvertTo-Json $body

    Write-DosMessage -Level "Information" -Message "Registering Fabric.Identity Client."
    $identityClientSecret = ([string](Add-ClientRegistration -authUrl $identityServerUrl -body $jsonBody -accessToken $accessToken)).Trim()
    return $identityClientSecret
}

function Add-SecureIdentityEnvironmentVariables([System.Security.Cryptography.X509Certificates.X509Certificate2] $encryptionCert, [string] $identityClientSecret, [string] $registrationApiSecret, [string] $appDirectory){
    $environmentVariables = @{}
    if($identityClientSecret){
        $encryptedSecret = Get-EncryptedString $encryptionCert $identityClientSecret
        $environmentVariables.Add("IdentityServerConfidentialClientSettings__ClientSecret", $encryptedSecret)
    }
    
    if($registrationApiSecret){
        $encryptedSecret = Get-EncryptedString $encryptionCert $registrationApiSecret
        $environmentVariables.Add("IdentityServerApiSettings__ApiSecret", $encryptedSecret)
    }
    Set-EnvironmentVariables $appDirectory $environmentVariables | Out-Null
}

function Test-RegistrationComplete([string] $authUrl)
{
    $url = "$authUrl/api/client/fabric-installer"
    $headers = @{"Accept" = "application/json"}
    
    try {
        Invoke-RestMethod -Method Get -Uri $url -Headers $headers
    } catch {
        $exception = $_.Exception
    }

    if($null -ne $exception -and $exception.Response.StatusCode.value__ -eq 401)
    {
        Write-DosMessage -Level "Information" -Message "Fabric registration is already complete."
        return $true
    }

    return $false
}

function Test-MeetsMinimumRequiredPowerShellVerion([int] $majorVersion){
    if($PSVersionTable.PSVersion.Major -lt $majorVersion){
        Write-DosMessage -Level "Error" -Message "PowerShell version $majorVersion is the minimum required version to run this installation. PowerShell version $($PSVersionTable.PSVersion) is currently installed."
        throw
    }
}

function Add-DatabaseLogin([string] $userName, [string] $connString)
{
    $query = "USE master
            If Not exists (SELECT * FROM sys.server_principals
                WHERE sid = suser_sid(@userName))
            BEGIN
                print '-- creating database login'
                DECLARE @sql nvarchar(4000)
                set @sql = 'CREATE LOGIN ' + QUOTENAME('$userName') + ' FROM WINDOWS'
                EXEC sp_executesql @sql
            END"
    try {
        Invoke-Sql $connString $query @{userName=$userName} | Out-Null
    }
    catch {
        Write-DosMessage -Level "Fatal" -Message "An error occurred while executing the command. Connection String: $($connString). Error $($_.Exception)"
    }
}

function Add-DatabaseUser([string] $userName, [string] $connString)
{
    $query = "IF( NOT EXISTS (SELECT * FROM sys.database_principals WHERE name = @userName))
            BEGIN
                print '-- Creating user';
                DECLARE @sql nvarchar(4000)
                set @sql = 'CREATE USER ' + QUOTENAME('$userName') + ' FOR LOGIN ' + QUOTENAME('$userName')
                EXEC sp_executesql @sql
            END"
    try {
        Invoke-Sql $connString $query @{userName=$userName} | Out-Null
    }
    catch {
        Write-DosMessage -Level "Fatal" -Message "An error occurred while executing the command. Connection String: $($connString). Error $($_.Exception)"
    }
}

function Add-DatabaseUserToRole([string] $userName, [string] $connString, [string] $role)
{
    $query = "DECLARE @exists int
            SELECT @exists = IS_ROLEMEMBER(@role, @userName) 
            IF (@exists IS NULL OR @exists = 0)
            BEGIN
                print '-- Adding @role to @userName';
                EXEC sp_addrolemember @role, @userName;
            END"
    try {
        Invoke-Sql $connString $query @{userName=$userName; role=$role} | Out-Null
    }
    catch {
        Write-DosMessage -Level "Fatal" -Message "An error occurred while executing the command. Connection String: $($connString). Error $($_.Exception)"
    }
}
function Add-ServiceUserToDiscovery([string] $userName, [string] $connString){

    $query = "DECLARE @IdentityID int;
                DECLARE @DiscoveryServiceUserRoleID int;

                SELECT @IdentityID = IdentityID FROM CatalystAdmin.IdentityBASE WHERE IdentityNM = @userName;
                IF (@IdentityID IS NULL)
                BEGIN
                    print ''-- Adding Identity'';
                    INSERT INTO CatalystAdmin.IdentityBASE (IdentityNM) VALUES (@userName);
                    SELECT @IdentityID = SCOPE_IDENTITY();
                END

                SELECT @DiscoveryServiceUserRoleID = RoleID FROM CatalystAdmin.RoleBASE WHERE RoleNM = 'DiscoveryServiceUser';
                IF (NOT EXISTS (SELECT 1 FROM CatalystAdmin.IdentityRoleBASE WHERE IdentityID = @IdentityID AND RoleID = @DiscoveryServiceUserRoleID))
                BEGIN
                    print ''-- Assigning Discovery Service user'';
                    INSERT INTO CatalystAdmin.IdentityRoleBASE (IdentityID, RoleID) VALUES (@IdentityID, @DiscoveryServiceUserRoleID);
                END"
    try {
        Invoke-Sql $connString $query @{userName=$userName} | Out-Null
    }
    catch {
        Write-DosMessage -Level "Fatal" -Message "An error occurred while executing the command. Connection String: $($connString). Error $($_.Exception)"
    }
}

function Restart-W3SVC(){
    net stop was /y
    net start w3svc
}

function Test-ShouldShowCertMenu([string] $primarySigningCertificateThumbprint, [string] $encryptionCertificateThumbprint, [bool] $quiet){
    return !$quiet -and ([string]::IsNullOrWhitespace($encryptionCertificateThumbprint) -or [string]::IsNullOrWhitespace($primarySigningCertificateThumbprint))
}

function Get-DefaultDiscoveryServiceUrl([string] $discoUrl)
{
    if([string]::IsNullOrEmpty($discoUrl)){
        return "$(Get-FullyQualifiedMachineName)/DiscoveryService/v1"
    }else{
        $discoUrl = $discoUrl.TrimEnd("/")
          if ($discoUrl -notmatch "/v\d")
          {
              return $discoUrl + "/v1"
          }
          else 
          {
              return $discoUrl
          }
    }
}

function Get-DefaultApplicationEndpoint([string] $appName, [string] $appEndPoint)
{
    if([string]::IsNullOrEmpty($appEndPoint)){
        return "$(Get-FullyQualifiedMachineName)/$appName"
    }else{
        return $appEndPoint
    }
}

function Get-FullyQualifiedMachineName() {
    return "https://$env:computername.$((Get-WmiObject Win32_ComputerSystem).Domain.tolower())"
}

function Get-ApplicationUrl($serviceName, $discoveryServiceUrl){
    $discoveryRequest = "$discoveryServiceUrl/Services?`$filter=ServiceName eq '$serviceName'&`$select=ServiceUrl&`$orderby=Version desc"
    $discoveryResponse = Invoke-RestMethod -Method Get -Uri $discoveryRequest -UseDefaultCredentials
    $serviceUrl = $discoveryResponse.value.ServiceUrl
    if([string]::IsNullOrWhiteSpace($serviceUrl)){
        $addToError = "There was an error getting the service registration for $serviceName, using DiscoveryService url $discoveryRequest."
        throw "The service $serviceName and $serviceUrl is not registered with the Discovery service. $addToError Make sure that this version of the service is registered w/ Discovery service before proceeding. Halting installation."
    }
    return $serviceUrl
}

function Get-WebApplicationFromDiscovery {
    param(
        [string] $applicationName,
        [Uri] $discoveryServiceUrl,
        [bool] $noDiscoveryService,
        [bool] $quiet
    )
    if($noDiscoveryService) {
        $serviceUrl = Get-DefaultApplicationEndpoint $applicationName $null
    }
    else {
        $serviceUrl = Get-ApplicationUrl -serviceName $applicationName -discoveryServiceUrl $discoveryServiceUrl

        if([string]::IsNullOrWhiteSpace($serviceUrl) -and $quiet) {
            $serviceUrl = Get-DefaultApplicationEndpoint $applicationName $null
        }
        elseif([string]::IsNullOrWhiteSpace($serviceUrl)){
            throw "Could not retrieve a registered URL from DiscoveryService for $applicationName Halting installation."
        }
    }

    $serviceUri = [System.Uri]$serviceUrl

    $serviceUrlSegments = $serviceUri.AbsolutePath.Split("/", [System.StringSplitOptions]::RemoveEmptyEntries)
    $serviceRootPath = "/$($serviceUrlSegments[0])"

    $app = Get-WebApplication | Where-Object {$_.Path -eq $serviceRootPath}
    
    if($null -eq $app){
        throw "Could not find an installed application that matches the path of the registered URL: $serviceUrl, at application path: $serviceRootPath. Halting installation."
    }

    return $app
}

function Get-WebConfigPath {
    param(
        [string] $applicationName,
        [Uri] $discoveryServiceUrl,
        [bool] $noDiscoveryService,
        [bool] $quiet
    )
    $app = Get-WebApplicationFromDiscovery -applicationName $applicationName -discoveryServiceUrl $discoveryServiceUrl -noDiscoveryService $noDiscoveryService -quiet $quiet

    $appPath = [Environment]::ExpandEnvironmentVariables($app.PhysicalPath)

    $configPath = [System.IO.Path]::Combine($appPath, "web.config")
    
    if(!(Test-Path $configPath)){
        throw "Could not find a web.config in $appPath for the $($app.Path) application. Halting installation."
    }

    return $configPath
}

function Get-ClientSettingsFromInstallConfig {
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

    $clientSettings = New-Object System.Collections.Generic.List[HashTable]
    foreach($tenant in $tenants.variable) {
      if ($tenant.appName -eq $appName)
      {
        $tenantSetting = @{
            clientId = $tenant.clientId
            # Does not decrypt secret
            clientSecret = $tenant.secret
            tenantId = $tenant.tenantId
            tenantAlias = $tenant.tenantAlias
        }
        $clientSettings.Add($tenantSetting)
      }
    }

    return $clientSettings
}

function Get-SettingsFromInstallConfig {
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

    return Get-TenantSettingsFromInstallConfig -installConfigPath $installConfigPath -scope $scope -setting $setting
}

function Get-TenantSettingsFromInstallConfig {
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
        $settingList += @{name = $nodeChild.name; alias = $nodeChild.alias}
    }
    return $settingList
}

function Add-InstallationTenantSettings {
    param(
        [Parameter(Mandatory=$true)]
        [string] $configSection,
        [Parameter(Mandatory=$true)]
        [string] $tenantId,
        [Parameter(Mandatory=$true)]
        [string] $tenantAlias,
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
        
        $nameAttribute = $installationConfig.CreateAttribute("tenantAlias")
        $nameAttribute.Value = $tenantAlias
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


function Set-IdentityEnvironmentAzureVariables {
    param (
        [string] $appConfig,
        [string] $installConfigPath,
        [string] $useAzure = $false,
        [string] $useWindows = $true,
        [System.Security.Cryptography.X509Certificates.X509Certificate2] $encryptionCert
    )
    $environmentVariables = @{}

    if($useAzure -eq $true)
    {
        $scope = "identity"
        # Alter Identity web.config for azure
        $clientSettings = Get-ClientSettingsFromInstallConfig -installConfigPath $installConfigPath -appName "Identity Service"
        $allowedTenants += Get-TenantSettingsFromInstallConfig -installConfigPath $installConfigPath `
            -scope $scope `
            -setting "allowedTenants"

        $claimsIssuer += Get-TenantSettingsFromInstallConfig -installConfigPath $installConfigPath `
            -scope $scope `
            -setting "claimsIssuerTenant"

        # Set Azure Settings
        $environmentVariables.Add("AzureActiveDirectorySettings__Authority", "https://login.microsoftonline.com/common")
        $environmentVariables.Add("AzureActiveDirectorySettings__DisplayName", "Azure AD")
        $environmentVariables.Add("AzureActiveDirectorySettings__ClaimsIssuer", "https://login.microsoftonline.com/" + $claimsIssuer.name)
        $environmentVariables.Add("AzureActiveDirectorySettings__TenantAlias", $claimsIssuer.alias)
        $environmentVariables.Add("AzureActiveDirectorySettings__Scope__0", "openid")
        $environmentVariables.Add("AzureActiveDirectorySettings__Scope__1", "profile")
        $environmentVariables.Add("AzureActiveDirectorySettings__ClientId", $clientSettings.clientId)

        $secret = $clientSettings.clientSecret
            if($secret -is [string] -and !$secret.StartsWith("!!enc!!:")){
                $encryptedSecret = Get-EncryptedString  $encryptionCert $secret
                # Encrypt secret in install.config if not encrypted
                Add-InstallationTenantSettings -configSection "identity" `
                    -tenantId $clientSettings.tenantId `
                    -tenantAlias $clientSettings.tenantAlias `
                    -clientSecret $encryptedSecret `
                    -clientId $clientSettings.clientId `
                    -installConfigPath $installConfigPath `
                    -appName "Identity Service"

                $environmentVariables.Add("AzureActiveDirectorySettings__ClientSecret", $encryptedSecret)
            }
            else{
                $environmentVariables.Add("AzureActiveDirectorySettings__ClientSecret", $secret)
            }

        $index = 0
        foreach($allowedTenant in $allowedTenants)
        {
          $environmentVariables.Add("AzureActiveDirectorySettings__IssuerWhiteList__$index", "https://sts.windows.net/" + $allowedTenant.name + "/")
          $environmentVariables.Add("AzureActiveDirectorySettings__TenantAlias__$index", $allowedTenant.alias)
          $index++
        }
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

function Clear-IdentityProviderSearchServiceWebConfigAzureSettings {
    param(
        [string] $webConfigPath
    )
    $content = [xml](Get-Content $webConfigPath)
    $settings = $content.configuration.appSettings

    $azureSettings = ($settings.ChildNodes | Where-Object {$null -ne $_.Key -and $_.Key.StartsWith("AzureActiveDirectoryClientSettings") -or $_.Key -eq ("EncryptionCertificateSettings:EncryptionCertificateThumbprint")})
    
    foreach($setting in $azureSettings) {
        Write-Host "Cleaning up setting: $($setting.key)"
        $settings.RemoveChild($setting) | Out-Null
    }
    $content.Save("$webConfigPath")

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

function Set-WebConfigAppSettings($webConfigPath, $appSettings){
    Write-Host "Writing app settings to web config..."
    $webConfig = [xml](Get-Content $webConfigPath)
    foreach ($variable in $appSettings.GetEnumerator()){
        Add-AppSetting $variable.Name $variable.Value $webConfig
    }

    $webConfig.Save("$webConfigPath")
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
    Write-Host "Setting IdPSS Web Config Settings."
    Clear-IdentityProviderSearchServiceWebConfigAzureSettings -webConfigPath $webConfigPath
    $appSettings = @{}
    
    if($appInsightsInstrumentationKey){
        $appSettings.Add("ApplicationInsights:Enabled", "true")
        $appSettings.Add("ApplicationInsights:InstrumentationKey", $appInsightsInstrumentationKey)
    }

    if ($useAzure -eq $true)
    {
        # Alter IdPSS web.config for azure
        $clientSettings = @()
        $clientSettings += Get-ClientSettingsFromInstallConfig -installConfigPath $installConfigPath -appName $appName

        if ($encryptionCertificateThumbprint){
        $appSettings.Add("EncryptionCertificateSettings:EncryptionCertificateThumbprint", $encryptionCertificateThumbprint)
        }

        # Set Azure Settings
        $defaultScope = "https://graph.microsoft.com/.default"
        $appSettings.Add("AzureActiveDirectoryClientSettings:Authority", "https://login.microsoftonline.com/")
        $appSettings.Add("AzureActiveDirectoryClientSettings:TokenEndpoint", "/oauth2/v2.0/token")

        foreach($setting in $clientSettings) {
            $index = $clientSettings.IndexOf($setting)
            $appSettings.Add("AzureActiveDirectoryClientSettings:ClientAppSettings:$index`:ClientId", $setting.clientId)
            $appSettings.Add("AzureActiveDirectoryClientSettings:ClientAppSettings:$index`:TenantId", $setting.tenantId)
            $appSettings.Add("AzureActiveDirectoryClientSettings:ClientAppSettings:$index`:TenantAlias", $setting.tenantAlias)

            # Currently only a single default scope is expected
            $appSettings.Add("AzureActiveDirectoryClientSettings:ClientAppSettings:$index`:Scopes:0", $defaultScope)

            $secret = $setting.clientSecret
            if($secret -is [string] -and !$secret.StartsWith("!!enc!!:")){
                $encryptedSecret = Get-EncryptedString  $encryptionCert $secret
                # Encrypt secret in install.config if not encrypted
                Add-InstallationTenantSettings -configSection "identity" `
                    -tenantId $setting.tenantId `
                    -tenantAlias $setting.tenantAlias `
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

    Write-Host "Web Config Path: $($webConfigPath)"

    Set-WebConfigAppSettings $webConfigPath $appSettings | Out-Null
}

function Find-IISAppPoolUser {
    param(
        [string] $applicationName,
        [Uri] $discoveryServiceUrl,
        [bool] $noDiscoveryService,
        [bool] $quiet
    )
    $app = Get-WebApplicationFromDiscovery -applicationName $applicationName -discoveryServiceUrl $discoveryServiceUrl -noDiscoveryService $noDiscoveryService -quiet $quiet
    $appPoolName = $app.applicationPool

    if($null -eq $appPoolName) {
        Write-DosMessage -Level "Fatal" -Message "Could not find any application named `"$applicationName`""
    }
    $appPool = (Get-Item (Join-Path 'IIS:\AppPools\' $appPoolName))

    if($appPool.processModel.identityType -eq 'ApplicationPoolIdentity') {
        Write-DosMessage -Level "Fatal" -Message "Application Pool users of identity type `"ApplicationPoolIdentity`" are not allowed."
    }

    $username = $appPool.processModel.username

    if($null -eq $username -or [string]::IsNullOrEmpty($username)) {
        Write-DosMessage -Level "Fatal" -Message "Could not find user for application `"$applicationName`" with application pool `"$appPoolName`". Please verify that the application pool has a valid user."
    }
    return $username
}

function Set-LoggingConfiguration{
    param(
        [Parameter(Mandatory=$true)]
        [Hashtable] $commonConfig
    )
    if(!([string]::IsNullOrEmpty($commonConfig.minimumLoggingLevel)) -and !([string]::IsNullOrEmpty($commonConfig.logFilePath))){
        Set-DosMessageConfiguration -LoggingMode Both -MinimumLoggingLevel $commonConfig.minimumLoggingLevel -LogFilePath $commonConfig.logFilePath
    }
}

function Set-IdentityUri {
    param(
        [Parameter(Mandatory=$true)]
        [string] $identityUri,
        [Parameter(Mandatory=$true)]
        [string] $connString
    )

    $query = "DECLARE
                 @AttributeNM VARCHAR(255) = 'IdentityUri';
              
              IF NOT EXISTS (SELECT
                               1
                             FROM
                               CatalystAdmin.AttributeBASE
                             WHERE
                               AttributeNM = @AttributeNM)
              BEGIN
              
                INSERT INTO
                  CatalystAdmin.AttributeBASE
                    (AttributeNM
                    ,AttributeDSC
                    ,AttributeTypeCD
                    ,CapSystemFLG)
                VALUES
                  (@AttributeNM
                  ,'Provides the url connection address for Identity'
                  ,'string'
                  ,1);
              
              END
              
              IF EXISTS (SELECT
                           1
                         FROM
                           CatalystAdmin.ObjectAttributeBASE
                         WHERE
                           AttributeNM = @AttributeNM)
              BEGIN
              
                UPDATE
                  CatalystAdmin.ObjectAttributeBASE
                SET
                  AttributeValueTXT = @identityUri
                WHERE
                  AttributeNM = @AttributeNM;
              
              END
              ELSE
              BEGIN
              
                INSERT INTO
                  CatalystAdmin.ObjectAttributeBASE
                    (ObjectID
                    ,ObjectTypeCD
                    ,AttributeNM
                    ,AttributeTypeCD
                    ,AttributeValueTXT)
                VALUES
                  (0
                  ,'System'
                  ,@AttributeNM
                  ,'string'
                  ,@identityUri);
              
              END;"

    try {
        Invoke-Sql $connString $query @{identityUri=$identityUri} | Out-Null
    }
    catch {
        Write-DosMessage -Level "Fatal" -Message "An error occurred while executing the command. Connection String: $($connString). Error $($_.Exception)"
    }
}

function Get-WebDeployPackagePath{
    param(
        [Parameter(Mandatory=$true)]
        [string] $standalonePath,
        [Parameter(Mandatory=$true)]
        [string] $installerPath
    )
    if(Test-Path $standalonePath){
        return Resolve-Path $standalonePath
    }
    if(Test-Path $installerPath){
        return Resolve-Path $installerPath
    }
    Write-DosMessage -Level "Fatal" -Message "Could not find the web deploy package at $standalonePath or $installerPath."
}

function Get-IdpssWebDeployParameters{
    param(
        [Parameter(Mandatory=$true)]
        [Hashtable] $serviceConfig,
        [Parameter(Mandatory=$true)]
        [Hashtable] $commonConfig,
        [string] $applicationEndpoint,
        [string] $discoveryServiceUrl,
        [string] $noDiscoveryService,
        [string] $registrationApiSecret,
        [string] $metaDataConnectionString,
        [string] $currentDomain
    )

    Confirm-ServiceConfig -commonConfig $commonConfig -serviceConfig $serviceConfig 
    
    $webDeployParameters = @(
                                @{
                                    Name = "IIS Web Application Name";
                                    Value = "$($serviceConfig.siteName)/$($serviceConfig.appName)"
                                },
                                @{
                                    Name = "Application Endpoint Address";
                                    Value = "$applicationEndpoint"
                                },
                                @{
                                    Name =  "MetadataContext-Deployment-Deployment Connection String";
                                    Value = $metaDataConnectionString
                                },
                                @{
                                    Name = "MetadataContext-Web.config Connection String";
                                    Value = $metaDataConnectionString
                                },
                                @{
                                    Name = "Current Domain";
                                    Value = $currentDomain
                                },
                                @{
                                    Name = "Identity Provider Search Service Api Secret";
                                    Value = $registrationApiSecret
                                },
                                @{
                                    Name = "Discovery Service Endpoint";
                                    Value = $commonConfig.DiscoveryService
                                }
                            )

    if(!$noDiscoveryService)
    {
     [HashTable] $discovery = @{Name = "Discovery Service Endpoint"; Value = $discoveryServiceUrl}
     $webDeployParameters += $discovery
    }

    return $webDeployParameters
}

function Confirm-ServiceConfig{
    param(
        [Parameter(Mandatory=$true)]
        [Hashtable] $commonConfig,
        [Parameter(Mandatory=$true)]
        [Hashtable] $serviceConfig
    )

    Confirm-SettingIsNotNull -settingName "common.sqlServerAddress" -settingValue $commonConfig.sqlServerAddress
    Confirm-SettingIsNotNull -settingName "common.metadataDbName" -settingValue $commonConfig.metadataDbName
    Confirm-SettingIsNotNull -settingName "$serviceConfig.appName" -settingValue $serviceConfig.appName
    Confirm-SettingIsNotNull -settingName "$serviceConfig.appPoolName" -settingValue $serviceConfig.appPoolName
    Confirm-SettingIsNotNull -settingName "$serviceConfig.siteName" -settingValue $serviceConfig.siteName
}

function Confirm-SettingIsNotNull{
    param(
        [Parameter(Mandatory=$true)]
        [string] $settingName,
        [string] $settingValue
    )

    if([string]::IsNullOrEmpty($settingValue)){
        Write-DosMessage -Level "Fatal" -Message "You must specify a valid $settingName in config."
    }
}

function New-LogsDirectoryForApp($appDirectory, $iisUser){
    # Create the necessary directories for the app
    $logDirectory = "$appDirectory\logs"

    if(!(Test-Path $appDirectory)) {
        Write-Console "Creating application directory: $appDirectory."
        mkdir $appDirectory | Out-Null
    }else{
        Write-Console "Application directory: $appDirectory exists."
    }

    
    if(!(Test-Path $logDirectory)) {
        Write-Console "Creating application log directory: $logDirectory."
        mkdir $logDirectory | Out-Null
        Write-Console "Setting Write and Read access for $iisUser on $logDirectory."
        $acl = Get-Acl $logDirectory
        $writeAccessRule = New-Object System.Security.AccessControl.FileSystemAccessRule($iisUser, "Write", "ContainerInherit,ObjectInherit", "None", "Allow")
        $readAccessRule = New-Object System.Security.AccessControl.FileSystemAccessRule($iisUser, "Read", "ContainerInherit,ObjectInherit", "None", "Allow")

        try {			
            $acl.AddAccessRule($writeAccessRule)
        } catch [System.InvalidOperationException]
        {
            # Attempt to fix parent identity directory before log directory
            RepairAclCanonicalOrder(Get-Acl $appDirectory)
            RepairAclCanonicalOrder($acl)
            $acl.AddAccessRule($writeAccessRule)
        }
        
        try {
            $acl.AddAccessRule($readAccessRule)
        } catch [System.InvalidOperationException]
        {
            RepairAclCanonicalOrder($acl)
            $acl.AddAccessRule($readAccessRule)
        }
        
        try {
            Set-Acl -Path $logDirectory $acl
        } catch [System.InvalidOperationException]
        {
            RepairAclCanonicalOrder($acl)
            Set-Acl -Path $logDirectory $acl
        }
    }else{
        Write-Console "Log directory: $logDirectory exists"
    }
}

function Get-CurrentUserDomain
{
    param(
        [Parameter(Mandatory=$true)]
        [bool] $quiet
    )
    $DNSForestName = (Get-WmiObject -Class Win32_ComputerSystem).Domain
    $currentUserDomain = (Get-WmiObject Win32_NTDomain -Filter "DnsForestName = '$DNSForestName'").DomainName
    if(!$quiet){
        $userEnteredDomain = Read-Host "Press Enter to accept the default domain '$($currentUserDomain)' that the user/group who will administrate dos is a member or enter a new domain" 
        if (![string]::IsNullOrEmpty($userEnteredDomain)) {
            $currentUserDomain = $userEnteredDomain
        }
    }
    return $currentUserDomain
}

function Get-WebDeployParameters{
    param(
        [Parameter(Mandatory=$true)]
        [Hashtable] $discoveryConfig,
        [Parameter(Mandatory=$true)]
        [Hashtable] $commonConfig,
        [Parameter(Mandatory=$true)]
        [string] $userName,
        [string] $registrationApiSecret
    )

    Confirm-DiscoveryConfig -discoveryConfig $discoveryConfig -commonConfig $commonConfig
    $metaDataConnectionString = Get-MetadataConnectionStringForDiscovery -commonConfig $commonConfig

    if([string]::IsNullOrEmpty($commonConfig.webServerDomain))
    {
      Write-DosMessage -Level "Error" -Message "The Fabric Identity URL: '$($commonConfig.IdentityService)' is not valid.  Check the install.config, section 'common', name 'IdentityService' for further details." -ErrorAction Stop
    }
    
    $webDeployParameters = @(
                                @{
                                    Name = "IIS Web Application Name";
                                    Value = "$($discoveryConfig.siteName)/$($discoveryConfig.appName)"
                                },
                                @{
                                    Name = "App Pool Account";
                                    Value = $userName
                                },
                                @{
                                    Name = "Application Endpoint Address";
                                    Value = "https://$($commonConfig.webServerDomain)/$($discoveryConfig.appName)"
                                },
                                @{
                                    Name = "Client Environment";
                                    Value = "$($commonConfig.clientEnvironment)"
                                },
                                @{
                                    Name = "DiscoveryServiceDataContext-Deployment-Deployment Connection String";
                                    Value = $metaDataConnectionString
                                },
                                @{
                                    Name = "DiscoveryServiceDataContext-Web.config Connection String";
                                    Value = $metaDataConnectionString
                                }
                            )

    if([string]::IsNullOrEmpty($registrationApiSecret) -ne $true) {
        $webDeployParameters += @{ 
            Name = "Discovery Service Api Secret"; 
            Value = $registrationApiSecret 
        }

        $webDeployParameters += @{
            Name = "Fabric.Identity URL";
            Value = "$($commonConfig.IdentityService)"
        }
    }
    
    return $webDeployParameters
}

function Confirm-DiscoveryConfig{
    param(
        [Parameter(Mandatory=$true)]
        [Hashtable] $discoveryConfig,
        [Parameter(Mandatory=$true)]
        [Hashtable] $commonConfig
    )

    Confirm-SettingIsNotNull -settingName "common.sqlServerAddress" -settingValue $commonConfig.sqlServerAddress
    Confirm-SettingIsNotNull -settingName "common.metadataDbName" -settingValue $commonConfig.metadataDbName
    Confirm-SettingIsNotNull -settingName "common.webServerDomain" -settingValue $commonConfig.webServerDomain
    Confirm-SettingIsNotNull -settingName "common.clientEnvironment" -settingValue $commonConfig.clientEnvironment
    Confirm-SettingIsNotNull -settingName "discovery.appName" -settingValue $discoveryConfig.appName
    Confirm-SettingIsNotNull -settingName "discovery.appPoolName" -settingValue $discoveryConfig.appPoolName
    Confirm-SettingIsNotNull -settingName "discovery.siteName" -settingValue $discoveryConfig.siteName
}

function Add-DiscoveryApiResourceRegistration{
    param(
        [Parameter(Mandatory=$true)]
        [string] $identityServiceUrl, 
        [Parameter(Mandatory=$true)]
        [string] $fabricInstallerSecret
    )
    $accessToken = Get-AccessToken -identityUrl $identityServiceUrl -clientId "fabric-installer" -secret $fabricInstallerSecret -scope fabric/identity.manageresources

    Write-DosMessage -Level "Information" -Message "Registering Discovery Service API with Fabric.Identity..."
    $apiName = "discovery-service-api"
    try{
        [string]$apiSecret = [string]::Empty
        Write-Host "    Registering $($apiName) with Fabric.Identity"
        $body = New-APIRegistrationBody -apiName $apiName -userClaims @("name", "email", "roles", "group") -scopes @{"name" = "dos/discovery.read"} -isEnabled $true
        $apiSecret = New-ApiRegistration -identityUrl $identityServiceUrl -body (ConvertTo-Json $body) -accessToken $accessToken

        if (![string]::IsNullOrWhiteSpace($apiSecret)) {
		  return $apiSecret
        }
		else
		{
		  Write-DosMessage -Level "Error" -Message "Could not register api $($apiName), apiSecret is empty"
		}    
    }
    catch{
        Write-DosMessage -Level "Error" -Message "Could not register api $($apiName)"
        throw $_.Exception
    }
}

function Test-DiscoveryService{
    param(
        [Parameter(Mandatory=$true)]
        [ValidateNotNullOrEmpty()]
        [string] $discoveryBaseUrl
    )

    try{
        $requestUri = "$discoveryBaseUrl/v1/Services"
        Write-DosMessage -Level "Information" -Message "Testing installation of DiscoveryService at $requestUri"
        Invoke-RestMethod -Method Get -Uri $requestUri -UseDefaultCredentials | Out-Null
    } 
    catch [System.Net.WebException] {
        Write-DosMessage -Level "Error" -Message "Could not contact DiscoveryService at: $requestUri."
        $response = $_.Exception.Response
        if($null -ne $response){
            $errorDetails = Get-ErrorFromResponse -response $response
            Write-DosMessage -Level "Error" -Message "Status Code: $($errorDetails.StatusCode) - $($errorDetails.StatusDescription)."
            Write-DosMessage -Level "Error" -Message "Error Message: $($errorDetails.ErrorMessage)"
        }else{
            Write-DosMessage -Level "Error" -Message "$($_.Exception.Message)"
        }
        Write-DosMessage -Level "Fatal" -Message "Installation was not successful."
    }catch{
        Write-DosMessage -Level "Error" -Message "$($_.Exception.Message)"
        Write-DosMessage -Level "Fatal" -Message "Installation was not successful."
    }
}

Export-ModuleMember Get-FullyQualifiedInstallationZipFile
Export-ModuleMember Install-DotNetCoreIfNeeded
Export-ModuleMember Get-IISWebSiteForInstall
Export-ModuleMember Get-Certificates
Export-ModuleMember Get-IISAppPoolUser
Export-ModuleMember Add-PermissionToPrivateKey
Export-ModuleMember Get-AppInsightsKey
Export-ModuleMember Get-SqlServerAddress
Export-ModuleMember Get-IdentityDatabaseConnectionString
Export-ModuleMember Get-MetadataDatabaseConnectionString
Export-ModuleMember Get-DiscoveryServiceUrl
Export-ModuleMember Get-ApplicationEndpoint
Export-ModuleMember Unlock-ConfigurationSections
Export-ModuleMember Publish-Application
Export-ModuleMember Register-ServiceWithDiscovery
Export-ModuleMember Add-DatabaseSecurity
Export-ModuleMember Set-IdentityEnvironmentVariables
Export-ModuleMember Add-RegistrationApiRegistration
Export-ModuleMember Add-IdpssApiResourceRegistration
Export-ModuleMember Add-IdentityClientRegistration
Export-ModuleMember Add-SecureIdentityEnvironmentVariables
Export-ModuleMember Test-RegistrationComplete
Export-ModuleMember Add-InstallerClientRegistration
Export-ModuleMember Test-MeetsMinimumRequiredPowerShellVerion
Export-ModuleMember Get-WebConfigPath
Export-ModuleMember Set-IdentityEnvironmentAzureVariables
Export-ModuleMember Get-TenantSettingsFromInstallConfig
Export-ModuleMember Get-SettingsFromInstallConfig
Export-ModuleMember Add-InstallationTenantSettings
Export-ModuleMember Set-IdentityProviderSearchServiceWebConfigSettings
Export-ModuleMember Get-ClientSettingsFromInstallConfig
Export-ModuleMember Find-IISAppPoolUser
Export-ModuleMember Set-LoggingConfiguration
Export-ModuleMember Set-IdentityUri
Export-ModuleMember Get-WebDeployPackagePath
Export-ModuleMember Get-IdpssWebDeployParameters
Export-ModuleMember New-LogsDirectoryForApp
Export-ModuleMember Confirm-SettingIsNotNull
Export-ModuleMember Get-CurrentUserDomain

Export-ModuleMember Get-WebDeployParameters
Export-ModuleMember Add-DiscoveryApiResourceRegistration
Export-ModuleMember Test-DiscoveryService
