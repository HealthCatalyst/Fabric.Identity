#
# Install_Identity_Windows.ps1
#
param(
	[String]$zipPackage, 
	[String]$webroot, 
	[String]$appName, 
	[String]$iisUser, 
	[int]$portNumber, 
	[String]$hostHeader, 
	[String]$sslCertificateThumbprint,
	[String]$clientName,
	[String]$primarySigningCertificateThumbprint,
	[String]$secondarySigningCertificateThumbprint,
	[System.Uri]$elasticSearchServer,
	[String]$elasticSearchUsername,
	[String]$elasticSearchPassword,
	[String]$couchDbServer,
	[String]$couchDbUsername,
	[String]$couchDbPassword)

Import-Module WebAdministration
Add-Type -AssemblyName System.IO.Compression.FileSystem

function Add-EnvironmentVariable($variableName, $variableValue, $config){
	Write-Host "Writing $variableName to config"
	$environmentVariablesNode = $config.configuration.'system.webServer'.aspNetCore.environmentVariables
	$environmentVariable = $config.CreateElement("environmentVariable")
	
	$nameAttribute = $config.CreateAttribute("name")
	$nameAttribute.Value = $variableName
	$environmentVariable.Attributes.Append($nameAttribute)
	
	$valueAttribute = $config.CreateAttribute("value")
	$valueAttribute.Value = $variableValue
	$environmentVariable.Attributes.Append($valueAttribute)

	$environmentVariablesNode.AppendChild($environmentVariable)
}

function Create-AppRoot($appDirectory, $iisUser){
	# Create the necessary directories for the app
	$logDirectory = "$appDirectory\logs"

	Write-Host "Creating application directory: $appDirectory."
	if(!(Test-Path $appDirectory)) {mkdir $appDirectory}

	Write-Host "Creating applciation log directory: $logDirectory."
	if(!(Test-Path $logDirectory)) {
		mkdir $logDirectory
		Write-Host "Setting Write and Read access for $iisUser on $logDirectory."
		$acl = (Get-Item $logDirectory).GetAccessControl('Access')
		$writeAccessRule = New-Object System.Security.AccessControl.FileSystemAccessRule($iisUser, "Write", "ContainerInherit,ObjectInherit", "None", "Allow")
		$readAccessRule = New-Object System.Security.AccessControl.FileSystemAccessRule($iisUser, "Read", "ContainerInherit,ObjectInherit", "None", "Allow")
		$acl.AddAccessRule($writeAccessRule)
		$acl.AddAccessRule($readAccessRule)
		Set-Acl -Path $logDirectory $acl
	}
}

function Create-AppPool($appName){
	cd IIS:\AppPools

	if(!(Test-Path $appName -PathType Container))
	{
		Write-Host "AppPool $appName does not exist...creating."
		$appPool = New-WebAppPool $appName
		$appPool | Set-ItemProperty -Name "managedRuntimeVersion" -Value ""
		$appPool.Start()
	}
}

function Create-WebSite($appName, $portNumber, $appDirectory, $hostHeader){
	cd IIS:\Sites

	if(!(Test-Path $appName -PathType Container))
	{
		Write-Host "WebSite $appName does not exist...creating."
		$webSite = New-Website -Name $appName -Port $portNumber -Ssl -PhysicalPath $appDirectory -ApplicationPool $appName -HostHeader $hostHeader
	
		Write-Host "Assigning certificate..."
		$cert = Get-Item Cert:\LocalMachine\My\$sslCertificateThumbprint
		cd IIS:\SslBindings
		$sslBinding = "0.0.0.0!$portNumber"
		if(!(Test-Path $sslBinding)){
			$cert | New-Item $sslBinding
		}
	}
}

function Publish-WebSite($zipPackage, $appDirectory){
	# Extract the app into the app directory
	Write-Host "Extracting $zipPackage to $appDirectory."
	[System.IO.Compression.ZipFile]::ExtractToDirectory($zipPackage, $appDirectory)
	#Start-Sleep -Seconds 3
}

function Set-EnvironmentVariables($appDirectory, $environmentVariables){
	Write-Host "Writing environment variables to config..."
	$webConfig = [xml](Get-Content $appDirectory\web.config)
	foreach ($variable in $environmentVariables.GetEnumerator()){
		Add-EnvironmentVariable $variable.Name $variable.Value $webConfig
	}

	$webConfig.Save("$appDirectory\web.config")
}

function Encrypt-String($signingCert, $stringToEncrypt){
	$encryptedString = [System.Convert]::ToBase64String($signingCert.PublicKey.Key.Encrypt([System.Text.Encoding]::UTF8.GetBytes($stringToEncrypt), $true))
	return "!!enc!!:" + $encryptedString
}

# Install the .net core windows server hosting bundle
Write-Host "Installing dotnet core Windows Server hosting bundle..."
#Invoke-WebRequest -Uri https://go.microsoft.com/fwlink/?linkid=844461 -OutFile bundle.exe
#.\bundle.exe /quiet /install

$appDirectory = "$webroot\$appName"
Create-AppRoot $appDirectory $iisUser
Write-Host "App directory is: $appDirectory"
Create-AppPool $appName
Create-WebSite $appName $portNumber $appDirectory $hostHeader
Publish-WebSite $zipPackage $appDirectory


#Write environment variables
Write-Host "Loading up environment variables..."
$environmentVariables = @{"HostingOptions__UseInMemoryStores" = "false"; "HostingOptions__UseTestUsers" = "true"}
$signingCert = Get-Item Cert:\LocalMachine\My\$primarySigningCertificateThumbprint

if($clientName){
	$environmentVariables.Add("ClientName", $clientName)
}

if ($primarySigningCertificateThumbprint){
	$environmentVariables.Add("SigningCertificateSettings__PrimaryCertificateThumbprint", $primarySigningCertificateThumbprint)
}

if ($secondarySigningCertificateThumbprint){
	$environmentVariables.Add("SigningCertificateSettings__SecondaryCertificateThumbprint", $secondarySigningCertificateThumbprint)
}

if ($couchDbServer){
	$environmentVariables.Add("CouchDbSettings__Server", $couchDbServer)
}

if ($couchDbUsername){
	$environmentVariables.Add("CouchDbSettings__Username", $couchDbUsername)
}

if ($couchDbPassword){
	$encryptedCouchDbPassword = Encrypt-String $signingCert $couchDbPassword
	$environmentVariables.Add("CouchDbSettings__Password", $encryptedCouchDbPassword)
}

if($elasticSearchServer){
	$environmentVariables.Add("ElasticSearchSettings__Scheme", $elasticSearchServer.Scheme)
	$environmentVariables.Add("ElasticSearchSettings__Server", $elasticSearchServer.Host)
	$environmentVariables.Add("ElasticSearchSettings__Port", $elasticSearchServer.Port)
}

if($elasticSearchUsername){
	$environmentVariables.Add("ElasticSearchSettings__Username", $elasticSearchUsername)
}

if($elasticSearchPassword){
	$encryptedElasticSearchPassword = Encrypt-String $signingCert $elasticSearchPassword
	$environmentVariables.Add("ElasticSearchSettings__Password", $encryptedElasticSearchPassword)
}


Set-EnvironmentVariables $appDirectory $environmentVariables
