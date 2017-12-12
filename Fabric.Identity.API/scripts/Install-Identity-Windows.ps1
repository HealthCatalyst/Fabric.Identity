#
# Install_Identity_Windows.ps1
#	
param([bool]$overwriteWebConfig,
      [switch]$noDiscoveryService)

function Test-RegistrationComplete($authUrl)
{
    $url = "$authUrl/api/client/fabric-installer"
    $headers = @{"Accept" = "application/json"}
    
    try {
        Invoke-RestMethod -Method Get -Uri $url -Headers $headers
    } catch {
        $exception = $_.Exception
    }

    if($exception -ne $null -and $exception.Response.StatusCode.value__ -eq 401)
    {
        Write-Success "Fabric registration is already complete."
        return $true
    }

    return $false
}

function Add-DatabaseLogin($userName, $connString)
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
	Invoke-Sql $connString $query @{userName=$userName} | Out-Null
}

function Add-DatabaseUser($userName, $connString)
{
	$query = "IF( NOT EXISTS (SELECT * FROM sys.database_principals WHERE name = @userName))
			BEGIN
				print '-- Creating user';
				DECLARE @sql nvarchar(4000)
                set @sql = 'CREATE USER ' + QUOTENAME('$userName') + ' FOR LOGIN ' + QUOTENAME('$userName')
                EXEC sp_executesql @sql
			END"
	Invoke-Sql $connString $query @{userName=$userName} | Out-Null
}

function Add-DatabaseUserToRole($userName, $connString, $role)
{
	$query = "DECLARE @exists int
			SELECT @exists = IS_ROLEMEMBER(@role, @userName) 
			IF (@exists IS NULL OR @exists = 0)
			BEGIN
				print '-- Adding @role to @userName';
				EXEC sp_addrolemember @role, @userName;
			END"
	Invoke-Sql $connString $query @{userName=$userName; role=$role} | Out-Null
}

function Add-ServiceUserToDiscovery($userName, $connString){

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
	Invoke-Sql $connString $query @{userName=$userName} | Out-Null
}

function Add-DatabaseSecurity($userName, $role, $connString)
{
	Add-DatabaseLogin $userName $connString
	Add-DatabaseUser $userName $connString
	Add-DatabaseUserToRole $userName $connString $role
	Write-Success "Database security applied successfully"
}

function Add-DiscoveryRegistration($discoveryUrl, $serviceUrl, $credential)
{	
    $registrationBody = @{
        ServiceName = "IdentityService"
        Version = 1
        ServiceUrl = $serviceUrl
        DiscoveryType = "Service"
        IsHidden = $true
        FriendlyName = "Fabric.Identity"
        Description = "The Fabric.Identity service provides centralized authentication across the Fabric ecosystem."
        BuildNumber = "1.1.2017120101"
    }

	$url = "$discoveryUrl/v1/Services"
	$jsonBody = $registrationBody | ConvertTo-Json	
	try{
		Invoke-RestMethod -Method Post -Uri "$url" -Body "$jsonBody" -ContentType "application/json" -Credential $credential | Out-Null
		Write-Success "Fabric.Identity successfully registered with DiscoveryService."
	}catch{
		Write-Error "Unable to register Fabric.Identity with DiscoveryService. Error $($_.Exception.Message) Halting installation." -ErrorAction Stop
	}
}

if(!(Test-Path .\Fabric-Install-Utilities.psm1)){
    Invoke-WebRequest -Uri https://raw.githubusercontent.com/HealthCatalyst/InstallScripts/master/common/Fabric-Install-Utilities.psm1 -Headers @{"Cache-Control"="no-cache"} -OutFile Fabric-Install-Utilities.psm1
}
Import-Module -Name .\Fabric-Install-Utilities.psm1 -Force

if(!(Test-IsRunAsAdministrator))
{
    Write-Error "You must run this script as an administrator. Halting configuration." -ErrorAction Stop
}

function Unlock-ConfigurationSections(){   
    [System.Reflection.Assembly]::LoadFrom("$env:systemroot\system32\inetsrv\Microsoft.Web.Administration.dll") | Out-Null
    $manager = new-object Microsoft.Web.Administration.ServerManager      
    $config = $manager.GetApplicationHostConfiguration()

    $section = $config.GetSection("system.webServer/security/authentication/anonymousAuthentication")
    $section.OverrideMode = "Allow"    
    Write-Success "Unlocked system.webServer/security/authentication/anonymousAuthentication"

    $section = $config.GetSection("system.webServer/security/authentication/windowsAuthentication")
    $section.OverrideMode = "Allow"    
    Write-Success "Unlocked system.webServer/security/authentication/windowsAuthentication"
    
    $manager.CommitChanges()
}

function Invoke-Sql($connectionString, $sql, $parameters=@{}){    
    $connection = New-Object System.Data.SqlClient.SQLConnection($connectionString)
    $command = New-Object System.Data.SqlClient.SqlCommand($sql, $connection)
	
    try {
		foreach($p in $parameters.Keys){		
		  $command.Parameters.AddWithValue("@$p",$parameters[$p])
		 }

        $connection.Open()    
        $command.ExecuteNonQuery()
        $connection.Close()        
    }catch [System.Data.SqlClient.SqlException] {
        Write-Error "An error ocurred while executing the command. Please ensure the connection string is correct and the identity database has been setup. Connection String: $($connectionString). Error $($_.Exception.Message)"  -ErrorAction Stop
    }    
}

function Get-ApplicationEndpoint($appName)
{
    return "http://$env:computername/$appName"
}

function Get-DiscoveryServiceUrl()
{
    return "http://$env:computername/DiscoveryService"
}

function Add-PermissionToPrivateKey($iisUser, $signingCert, $permission){
    try{
        $allowRule = New-Object security.accesscontrol.filesystemaccessrule $iisUser, $permission, allow
        $keyFolder = "c:\programdata\microsoft\crypto\rsa\machinekeys"    

		$keyname = $signingCert.privatekey.cspkeycontainerinfo.uniquekeycontainername        
		$keyPath = [io.path]::combine($keyFolder, $keyname)		

        if ([io.file]::exists($keyPath))
        {        
            $acl = Get-Acl $keyPath
            $acl.AddAccessRule($allowRule)
            Set-Acl $keyPath $acl			
            Write-Success "The permission '$($permission)' was successfully added to the private key for user '$($iisUser)'"
        }else{
            Write-Error "No key file was found at '$($keyPath)'. Ensure a valid signing certificate was provided" -ErrorAction Stop
        }
    }catch{
        $scriptDirectory =  Get-CurrentScriptDirectory
        Set-Location $scriptDirectory
        Write-Error "There was an error adding the '$($permission)' permission for the user '$($iisUser)' to the private key. Error $($_.Exception.Message)" -ErrorAction Stop
    }	
}

$installSettings = Get-InstallationSettings "identity"
$zipPackage = $installSettings.zipPackage
$appName = $installSettings.appName
$primarySigningCertificateThumbprint = $installSettings.primarySigningCertificateThumbprint -replace '[^a-zA-Z0-9]', ''
$encryptionCertificateThumbprint = $installSettings.encryptionCertificateThumbprint -replace '[^a-zA-Z0-9]', ''
$appInsightsInstrumentationKey = $installSettings.appInsightsInstrumentationKey
$siteName = $installSettings.siteName
$sqlServerAddress = $installSettings.sqlServerAddress
$metadataDbName = $installSettings.metadataDbName
$identityDbName = $installSettings.identityDbName
$identityDatabaseRole = $installSettings.identityDatabaseRole
$discoveryServiceUrl = Get-DiscoveryServiceUrl
$applicationEndPoint = Get-ApplicationEndpoint $appName
$storedIisUser = $installSettings.iisUser
$useSpecificUser = $false;

$workingDirectory = Get-CurrentScriptDirectory

try{
    $sites = Get-ChildItem IIS:\Sites
    if($sites -is [array]){
        $sites |
            ForEach-Object {New-Object PSCustomObject -Property @{
                'Id'=$_.id;
                'Name'=$_.name;
                'Physical Path'=[System.Environment]::ExpandEnvironmentVariables($_.physicalPath);
                'Bindings'=$_.bindings;
            };} |
            Format-Table Id,Name,'Physical Path',Bindings -AutoSize

        $selectedSiteId = Read-Host "Select a web site by Id"
        Write-Host ""
        $selectedSite = $sites[$selectedSiteId - 1]
    }else{
        $selectedSite = $sites
    }

    $webroot = [System.Environment]::ExpandEnvironmentVariables($selectedSite.physicalPath)    
    $siteName = $selectedSite.name

}catch{
    Write-Error "Could not select a website." -ErrorAction Stop
}

try{

    $allCerts = Get-CertsFromLocation Cert:\LocalMachine\My
    $index = 1
    $allCerts |
        ForEach-Object {New-Object PSCustomObject -Property @{
        'Index'=$index;
        'Subject'= $_.Subject; 
        'Name' = $_.FriendlyName; 
        'Thumbprint' = $_.Thumbprint; 
        'Expiration' = $_.NotAfter
        };
        $index ++} |
        Format-Table Index,Name,Subject,Expiration,Thumbprint  -AutoSize

    $selectionNumber = Read-Host  "Select a signing and encryption certificate by Index"
    Write-Host ""
    if([string]::IsNullOrEmpty($selectionNumber)){
		Write-Error "You must select a certificate so Fabric.Identity can sign access and identity tokens." -ErrorAction Stop
    }
    $selectionNumberAsInt = [convert]::ToInt32($selectionNumber, 10)
    if(($selectionNumberAsInt -gt  $allCerts.Count) -or ($selectionNumberAsInt -le 0)){
        Write-Error "Please select a certificate with index between 1 and $($allCerts.Count)."  -ErrorAction Stop
    }
    $certThumbprint = Get-CertThumbprint $allCerts $selectionNumberAsInt     
    $primarySigningCertificateThumbprint = $certThumbprint -replace '[^a-zA-Z0-9]', ''    
    $encryptionCertificateThumbprint = $certThumbprint -replace '[^a-zA-Z0-9]', ''
    }catch{
        $scriptDirectory =  Get-CurrentScriptDirectory
        Set-Location $scriptDirectory
        Write-Error "Could not set the certificate thumbprint. Error $($_.Exception.Message)" -ErrorAction Stop        
}

try{
    $signingCert = Get-Certificate $primarySigningCertificateThumbprint
}catch{
    Write-Error "Could not get signing certificate with thumbprint $primarySigningCertificateThumbprint. Please verify that the primarySigningCertificateThumbprint setting in install.config contains a valid thumbprint for a certificate in the Local Machine Personal store."
    throw $_.Exception
}

try{
    $encryptionCert = Get-Certificate $encryptionCertificateThumbprint
}catch{
    Write-Error "Could not get encryption certificate with thumbprint $encryptionCertificateThumbprint. Please verify that the encryptionCertificateThumbprint setting in install.config contains a valid thumbprint for a certificate in the Local Machine Personal store."
    throw $_.Exception
}

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
    $useSpecificUser = $true
    $userEnteredPassword = Read-Host "Enter the password for $iisUser" -AsSecureString
    $credential = New-Object -TypeName "System.Management.Automation.PSCredential" -ArgumentList $iisUser, $userEnteredPassword
    [System.Reflection.Assembly]::LoadWithPartialName("System.DirectoryServices.AccountManagement") | Out-Null
    $ct = [System.DirectoryServices.AccountManagement.ContextType]::Domain
    $pc = New-Object System.DirectoryServices.AccountManagement.PrincipalContext -ArgumentList $ct,$credential.GetNetworkCredential().Domain
    $isValid = $pc.ValidateCredentials($credential.GetNetworkCredential().UserName, $credential.GetNetworkCredential().Password)
    if(!$isValid){
        Write-Error "Incorrect credentials for $iisUser" -ErrorAction Stop
    }
    Write-Success "Credentials are valid for user $iisUser"
    Write-Host ""
}else{
    Write-Error "No user account was entered, please enter a valid user account." -ErrorAction Stop
}

Add-PermissionToPrivateKey $iisUser $signingCert read

if((Test-Path $zipPackage))
{
    $path = [System.IO.Path]::GetDirectoryName($zipPackage)
    if(!$path)
    {
        $zipPackage = [System.IO.Path]::Combine($workingDirectory, $zipPackage)
        Write-Console "zipPackage: $zipPackage"
    }
}else{
    Write-Error "Could not find file or directory $zipPackage, please verify that the zipPackage configuration setting in install.config is the path to a valid zip file that exists." -ErrorAction Stop	
}


if(!(Test-PrerequisiteExact "*.NET Core*Windows Server Hosting*" 1.1.30503.82))
{    
    try{
        Write-Console "Windows Server Hosting Bundle version 1.1.30503.82 not installed...installing version 1.1.30503.82"        
        Invoke-WebRequest -Uri https://go.microsoft.com/fwlink/?linkid=848766 -OutFile $env:Temp\bundle.exe
        Start-Process $env:Temp\bundle.exe -Wait -ArgumentList '/quiet /install'
        net stop was /y
        net start w3svc			
    }catch{
        Write-Error "Could not install .NET Windows Server Hosting bundle. Please install the hosting bundle before proceeding. https://go.microsoft.com/fwlink/?linkid=844461" -ErrorAction Stop
    }
    try{
        Remove-Item $env:Temp\bundle.exe
    }catch{        
        $e = $_.Exception        
        Write-Warning "Unable to remove Server Hosting bundle exe" 
        Write-Warning $e.Message
    }

}else{
    Write-Success ".NET Core Windows Server Hosting Bundle installed and meets expectations."
    Write-Host ""
}

$userEnteredAppInsightsInstrumentationKey = Read-Host  "Enter Application Insights instrumentation key or hit enter to accept the default [$appInsightsInstrumentationKey]"
Write-Host ""

if(![string]::IsNullOrEmpty($userEnteredAppInsightsInstrumentationKey)){   
     $appInsightsInstrumentationKey = $userEnteredAppInsightsInstrumentationKey
}

$userEnteredSqlServerAddress = Read-Host "Press Enter to accept the default Sql Server address '$($sqlServerAddress)' or enter a new Sql Server address" 
Write-Host ""

if(![string]::IsNullOrEmpty($userEnteredSqlServerAddress)){
    $sqlServerAddress = $userEnteredSqlServerAddress
}

if(!($noDiscoveryService)){
    $userEnteredMetadataDbName = Read-Host "Press Enter to accept the default Metadata DB Name '$($metadataDbName)' or enter a new Metadata DB Name"
    if(![string]::IsNullOrEmpty($userEnteredMetadataDbName)){
        $metadataDbName = $userEnteredMetadataDbName
    }

    $metadataConnStr = "Server=$($sqlServerAddress);Database=$($metadataDbName);Trusted_Connection=True;MultipleActiveResultSets=True;"

    Invoke-Sql $metadataConnStr "SELECT TOP 1 RoleID FROM CatalystAdmin.RoleBASE" | Out-Null
    Write-Success "Metadata DB Connection string: $metadataConnStr verified"
    Write-Host ""
}

$userEnteredIdentityDbName = Read-Host "Press Enter to accept the default Identity DB Name '$($identityDbName)' or enter a new Identity DB Name"
if(![string]::IsNullOrEmpty($userEnteredIdentityDbName)){
    $identityDbName = $userEnteredIdentityDbName
}

$identityDbConnStr = "Server=$($sqlServerAddress);Database=$($identityDbName);Trusted_Connection=True;MultipleActiveResultSets=True;"

Invoke-Sql $identityDbConnStr "SELECT TOP 1 ClientId FROM Clients" | Out-Null
Write-Success "Identity DB Connection string: $identityDbConnStr verified"
Write-Host ""

if(!($noDiscoveryService)){
    $userEnteredDiscoveryServiceUrl = Read-Host "Press Enter to accept the default DiscoveryService URL [$discoveryServiceUrl] or enter a new URL"
    Write-Host ""
    if(![string]::IsNullOrEmpty($userEnteredDiscoveryServiceUrl)){   
         $discoveryServiceUrl = $userEnteredDiscoveryServiceUrl
    }
}


$userEnteredApplicationEndpoint = Read-Host "Press Enter to accept the default Application Endpoint URL [$applicationEndpoint] or enter a new URL"
Write-Host ""
if(![string]::IsNullOrEmpty($userEnteredApplicationEndpoint)){
    $applicationEndpoint = $userEnteredApplicationEndpoint
}

$identityServerUrl = $applicationEndpoint

if(!($noDiscoveryService)){
    Add-ServiceUserToDiscovery $credential.UserName $metadataConnStr
    Add-DiscoveryRegistration $discoveryServiceUrl $identityServerUrl $credential
    Write-Host ""
}

Unlock-ConfigurationSections
Write-Host ""

$appDirectory = "$webroot\$appName"
New-AppRoot $appDirectory $iisUser
Write-Console "App directory is: $appDirectory"
if($useSpecificUser){
    New-AppPool $appName $iisUser $credential
}else{
    New-AppPool $appName 
}

New-App $appName $siteName $appDirectory | Out-Null
Publish-WebSite $zipPackage $appDirectory $appName $overwriteWebConfig
Write-Host ""
Add-DatabaseSecurity $iisUser $identityDatabaseRole $identityDbConnStr

#Write environment variables
Write-Host ""
Write-Console "Loading up environment variables..."
$environmentVariables = @{"HostingOptions__StorageProvider" = "SqlServer"; "HostingOptions__UseTestUsers" = "false"; "AllowLocalLogin" = "false"}


if($clientName){
    $environmentVariables.Add("ClientName", $clientName)
}

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

Set-EnvironmentVariables $appDirectory $environmentVariables | Out-Null
Write-Host ""

Set-Location $workingDirectory



if(Test-RegistrationComplete $identityServerUrl)
{
    Write-Success "Installation complete, exiting."
    exit 0
}

#Register registration api
$body = @'
{
    "name":"registration-api",
    "userClaims":["name","email","role","groups"],
    "scopes":[{"name":"fabric/identity.manageresources"}, {"name":"fabric/identity.read"}, {"name":"fabric/identity.searchusers"}]
}
'@

Write-Console "Registering Fabric.Identity registration api."
$registrationApiSecret = Add-ApiRegistration -authUrl $identityServerUrl -body $body

#Register Fabric.Installer
$body = @'
{
    "clientId":"fabric-installer", 
    "clientName":"Fabric Installer", 
    "requireConsent":"false", 
    "allowedGrantTypes": ["client_credentials"], 
    "allowedScopes": ["fabric/identity.manageresources", "fabric/authorization.read", "fabric/authorization.write", "fabric/authorization.manageclients"]
}
'@

Write-Console "Registering Fabric.Installer."
$installerClientSecret = Add-ClientRegistration -authUrl $identityServerUrl -body $body

if($installerClientSecret){ Add-SecureInstallationSetting "common" "fabricInstallerSecret" $installerClientSecret $signingCert }
if($encryptionCertificateThumbprint){ Add-InstallationSetting "common" "encryptionCertificateThumbprint" $encryptionCertificateThumbprint }
if($encryptionCertificateThumbprint){ Add-InstallationSetting "identity" "encryptionCertificateThumbprint" $encryptionCertificateThumbprint }
if($appInsightsInstrumentationKey){ Add-InstallationSetting "identity" "appInsightsInstrumentationKey" "$appInsightsInstrumentationKey" }
if($sqlServerAddress){ Add-InstallationSetting "common" "sqlServerAddress" "$sqlServerAddress" }
if($metadataDbName){ Add-InstallationSetting "common" "metadataDbName" "$metadataDbName" }
if($identityDbName){ Add-InstallationSetting "identity" "identityDbName" "$identityDbName" }
if($siteName){ Add-InstallationSetting "identity" "siteName" $siteName }
if($primarySigningCertificateThumbprint){ Add-InstallationSetting "identity" "primarySigningCertificateThumbprint" $primarySigningCertificateThumbprint }
if($iisUser){ Add-InstallationSetting "identity" "iisUser" "$iisUser" }

Write-Console ""
Write-Console "Please keep the following secrets in a secure place:"
Write-Success "Fabric.Installer clientSecret: $installerClientSecret"
Write-Success "Fabric.Registration apiSecret: $registrationApiSecret"
Write-Console ""
Write-Console "The Fabric.Installer clientSecret will be needed in subsequent installations:"
Write-Success "Fabric.Installer clientSecret: $installerClientSecret"

Write-Success "Installation complete, exiting."
