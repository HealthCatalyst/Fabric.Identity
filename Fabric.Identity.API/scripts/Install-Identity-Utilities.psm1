# Import Fabric Install Utilities
$fabricInstallUtilities = ".\Fabric-Install-Utilities.psm1"
if (!(Test-Path $fabricInstallUtilities -PathType Leaf)) {
    Write-DosMessage -Level "Warning" -Message "Could not find fabric install utilities. Manually downloading and installing"
    Invoke-WebRequest -Uri https://raw.githubusercontent.com/HealthCatalyst/InstallScripts/master/common/Fabric-Install-Utilities.psm1 -Headers @{"Cache-Control" = "no-cache"} -OutFile $fabricInstallUtilities
}
Import-Module -Name $fabricInstallUtilities -Force

$dosInstallUtilities = Get-Childitem -Path ./**/DosInstallUtilities.psm1 -Recurse
if ($dosInstallUtilities.length -eq 0) {
    Install-Module DosInstallUtilities -Scope CurrentUser
    Import-Module DosInstallUtilities -Force
    Write-DosMessage -Level "Warning" -Message "Could not find dos install utilities. Manually installing"
}
else {
    Import-Module -Name $dosInstallUtilities.FullName
    Write-DosMessage -Level "Verbose" -Message "Installing DosInstallUtilities at $($dosInstallUtilities.FullName)"
}

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
            Invoke-WebRequest -Uri $downloadUrl -OutFile $env:Temp\bundle.exe
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
            Write-DosMessage -Level "Warning" -Message "Unable to remove temporary dowload file for server hosting bundle exe" 
            Write-DosMessage -Level "Warning" -Message  $e.Message
        }

    }else{
        Write-DosMessage -Level "Information" -Message  ".NET Core Windows Server Hosting Bundle (v$version) installed and meets expectations."
    }
}

function Get-IISWebSiteForInstall([string] $selectedSiteName, [bool] $quiet){
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

                $selectedSiteId = Read-Host "Select a web site by Id"
                $selectedSite = $sites[$selectedSiteId - 1]
            }else{
                $selectedSite = $sites
            }
        }
        if($null -eq $selectedSite){
            throw "Could not find selected site."
        }
        return $selectedSite

    }catch{
        Write-DosMessage -Level "Error" -Message "Could not select a website."
        throw
    }
}

function Restart-W3SVC(){
    net stop was /y
    net start w3svc
}

Export-ModuleMember Get-FullyQualifiedInstallationZipFile
Export-ModuleMember Install-DotNetCoreIfNeeded
Export-ModuleMember Get-IISWebSiteForInstall