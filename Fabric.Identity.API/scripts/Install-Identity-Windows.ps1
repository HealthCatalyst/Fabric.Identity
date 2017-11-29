#
# Install_Identity_Windows.ps1
#	

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
        Write-Host "Fabric registration is already complete."
        return $true
    }

    return $false
}

if(!(Test-Path .\Fabric-Install-Utilities.psm1)){
	Invoke-WebRequest -Uri https://raw.githubusercontent.com/HealthCatalyst/InstallScripts/master/common/Fabric-Install-Utilities.psm1 -Headers @{"Cache-Control"="no-cache"} -OutFile Fabric-Install-Utilities.psm1
}
Import-Module -Name .\Fabric-Install-Utilities.psm1 -Force

$installSettings = Get-InstallationSettings "identity"
$zipPackage = $installSettings.zipPackage
$appName = $installSettings.appName
$iisUser = $installSettings.iisUser
$primarySigningCertificateThumbprint = $installSettings.primarySigningCertificateThumbprint -replace '[^a-zA-Z0-9]', ''
$encryptionCertificateThumbprint = $installSettings.encryptionCertificateThumbprint -replace '[^a-zA-Z0-9]', ''
$appInsightsInstrumentationKey = $installSettings.appInsightsInstrumentationKey
$siteName = $installSettings.siteName
$hostUrl = $installSettings.hostUrl
$sqlServerAddress = $installSettings.sqlServerAddress
$sqlServerConnStr = $installSettings.sqlServerConnStr

$workingDirectory = Get-CurrentScriptDirectory


try{
	$sites = Get-ChildItem IIS:\Sites
	$sites |
		ForEach-Object {New-Object PSCustomObject -Property @{
			'Id'=$_.id;
			'Name'=$_.name;
			'Physical Path'=$_.physicalPath;
			'Bindings'=$_.bindings;
		};} |
		Format-Table Id,Name,'Physical Path',Bindings -AutoSize

	$selectedSiteId = Read-Host "Select a web site by Id:"

	if($sites -is [array]){
		$selectedSite = $sites[$selectedSiteId - 1]
	}else{
		$selectedSite = $sites
	}

	$webroot = $selectedSite.physicalPath
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

    $selectionNumber = Read-Host  "Select a certificate by Index"
    $certThumbprint = Get-CertThumbprint $allCerts $selectionNumber     
    $primarySigningCertificateThumbprint = $certThumbprint -replace '[^a-zA-Z0-9]', ''    
    $encryptionCertificateThumbprint = $certThumbprint -replace '[^a-zA-Z0-9]', ''
    }catch{
        Write-Host "Could not set the certificate thumbprint."
        throw $_.Exception
}


try{
	$signingCert = Get-Certificate $primarySigningCertificateThumbprint
}catch{
	Write-Host "Could not get signing certificate with thumbprint $primarySigningCertificateThumbprint. Please verify that the primarySigningCertificateThumbprint setting in install.config contains a valid thumbprint for a certificate in the Local Machine Personal store."
	throw $_.Exception
}

try{
	$encryptionCert = Get-Certificate $encryptionCertificateThumbprint
}catch{
	Write-Host "Could not get encryption certificate with thumbprint $encryptionCertificateThumbprint. Please verify that the encryptionCertificateThumbprint setting in install.config contains a valid thumbprint for a certificate in the Local Machine Personal store."
	throw $_.Exception
}

if((Test-Path $zipPackage))
{
	$path = [System.IO.Path]::GetDirectoryName($zipPackage)
	if(!$path)
	{
		$zipPackage = [System.IO.Path]::Combine($workingDirectory, $zipPackage)
		Write-Host "zipPackage: $zipPackage"
	}
}else{
	Write-Host "Could not find file or directory $zipPackage, please verify that the zipPackage configuration setting in install.config is the path to a valid zip file that exists."
	exit 1
}


if(!(Test-PrerequisiteExact "*.NET Core*Windows Server Hosting*" 1.1.30327.81))
{    
    try{
		Write-Host "Windows Server Hosting Bundle version 1.1.30327.81 not installed...installing version 1.1.30327.81"
        Write-Host "downloading to:" $env:Temp
		Invoke-WebRequest -Uri https://go.microsoft.com/fwlink/?linkid=844461 -OutFile $env:Temp\bundle.exe
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
    Write-Host ".NET Core Windows Server Hosting Bundle installed and meets expectations."
}

$userEnteredAppInsightsInstrumentationKey = Read-Host  "Enter Application Insights instrumentation key or hit enter to continue"

if(![string]::IsNullOrEmpty($userEnteredAppInsightsInstrumentationKey)){   
     $appInsightsInstrumentationKey = $userEnteredAppInsightsInstrumentationKey
}

$userEnteredSqlServerAddress = Read-Host "Press Enter to accept the default Sql Server address '$($sqlServerAddress)' or enter a new Sql Server address" 

if(![string]::IsNullOrEmpty($userEnteredSqlServerAddress)){    
    $sqlServerConnStr = "Server=$($userEnteredSqlServerAddress);Database=Identity;Trusted_Connection=True;MultipleActiveResultSets=True;"
}

$userEnteredSqlServerConnStr = Read-Host "Press Enter to accept the connection string '$($sqlServerConnStr)' or enter a new connection string"

if(![string]::IsNullOrEmpty($userEnteredSqlServerConnStr)){    
    $sqlServerConnStr = $userEnteredSqlServerConnStr
}


$appDirectory = "$webroot\$appName"
New-AppRoot $appDirectory $iisUser
Write-Host "App directory is: $appDirectory"
New-AppPool $appName
New-App $appName $siteName $appDirectory
Publish-WebSite $zipPackage $appDirectory $appName


#Write environment variables
Write-Host "Loading up environment variables..."
$environmentVariables = @{"HostingOptions__UseInMemoryStores" = "false"; "HostingOptions__UseTestUsers" = "false"; "AllowLocalLogin" = "false"}


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

$environmentVariables.Add("IdentityServerConfidentialClientSettings__Authority", "${hostUrl}/${appName}")

if($sqlServerConnStr){
    $environmentVariables.Add("ConnectionStrings__IdentityDatabase", $sqlServerConnStr)
}

Set-EnvironmentVariables $appDirectory $environmentVariables

Set-Location $workingDirectory
$identityServerUrl = "$hostUrl/identity"

if(Test-RegistrationComplete $identityServerUrl)
{
    Write-Host "Installation complete, exiting."
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

Write-Host "Registering Fabric.Identity registration api."
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

Write-Host "Registering Fabric.Installer."
$installerClientSecret = Add-ClientRegistration -authUrl $identityServerUrl -body $body
Add-SecureInstallationSetting "common" "fabricInstallerSecret" $installerClientSecret $signingCert

Write-Host ""
Write-Host "Please keep the following secrets in a secure place:"
Write-Host "Fabric.Installer clientSecret: $installerClientSecret"
Write-Host "Fabric.Registration apiSecret: $registrationApiSecret"
Write-Host ""
Write-Host "The Fabric.Installer clientSecret will be needed in subsequent installations:"
Write-Host "Fabric.Installer clientSecret: $installerClientSecret"

Write-Host "Installation complete, exiting."