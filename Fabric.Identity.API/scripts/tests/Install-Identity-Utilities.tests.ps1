param(
    [string] $targetFilePath = "$PSScriptRoot\..\Install-Identity-Utilities.psm1"
)

Write-Host $targetFilePath
# Force re-import to pick up latest changes
Import-Module $targetFilePath -Force
#Import-Module Pester -Force

Describe 'Get-FullyQualifiedInstallationZipFile Unit Tests' -Tag 'Unit' {
    Context 'Zip File Exists'{
        It 'Should return working directory plus zip file when no directory is specified'{
            Mock -ModuleName Install-Identity-Utilities -CommandName Test-Path -MockWith { return $true }
            $path = Get-FullyQualifiedInstallationZipFile -zipPackage "somefile.zip" -workingDirectory $PSScriptRoot
            $path | Should -Be "$PSScriptRoot\somefile.zip"
        }

        It 'Should return full path specified when full path is specified as a parameter'{
            Mock -ModuleName Install-Identity-Utilities -CommandName Test-Path -MockWith { return $true }
            $expectedPath = "$env:TEMP\somefile.zip"
            $path = Get-FullyQualifiedInstallationZipFile -zipPackage $expectedPath -workingDirectory $PSScriptRoot
            $path | Should -Be $expectedPath
        }
    }

    Context 'Zip File Does Not Exist'{
        It 'Should throw an exception if the zip file does not exist'{
            Mock -ModuleName Install-Identity-Utilities -CommandName Test-Path -MockWith {return $false}
            {Get-FullyQualifiedInstallationZipFile -zipPackage "somefile.zip" -workingDirectory $PSScriptRoot} | Should -Throw
        }
    }
}

Describe 'Install-DotNetCoreIfNeeded' -Tag 'Unit' {
    Context 'DotNetCore version is already installed'{
        It 'Should Not Install'{
            Mock -ModuleName Install-Identity-Utilities -CommandName Test-PrerequisiteExact -MockWith { return $true }
            Mock -ModuleName Install-Identity-Utilities -CommandName Invoke-WebRequest -MockWith {}
            Install-DotNetCoreIfNeeded -version "1.1.1234.123" -downloadUrl "http://example.com"
            Assert-MockCalled -ModuleName Install-Identity-Utilities -CommandName Invoke-WebRequest -Times 0 -Exactly

        }
        InModuleScope Install-Identity-Utilities {
            It 'Should install proper version' {
                Mock -CommandName Test-PrerequisiteExact -MockWith { return $false }
                Mock -CommandName Invoke-WebRequest -MockWith {}
                Mock -CommandName Start-Process -MockWith {}
                Mock -CommandName Restart-W3SVC -MockWith {}
                Mock -CommandName Remove-Item -MockWith {}
                Install-DotNetCoreIfNeeded -version "1.1.1234.123" -downloadUrl "http://example.com"
                Assert-MockCalled -CommandName Invoke-WebRequest -Times 1 -Exactly
                Assert-MockCalled -CommandName Start-Process -Times 1 -Exactly
                Assert-MockCalled -CommandName Restart-W3SVC -Times 1 -Exactly
                Assert-MockCalled -CommandName Remove-Item -Times 1 -Exactly
            }

            It 'Should throw an exception if Start-Process throws' {
                Mock -CommandName Test-PrerequisiteExact -MockWith { return $false }
                Mock -CommandName Invoke-WebRequest -MockWith {}
                Mock -CommandName Start-Process -MockWith { throw }
                Mock -CommandName Restart-W3SVC -MockWith {}
                Mock -CommandName Remove-Item -MockWith {}
                {Install-DotNetCoreIfNeeded -version "1.1.1234.123" -downloadUrl "http://example.com"} | Should -Throw
            }
        }
    }
}

Describe 'Get-IISWebSiteForInstall' -Tag 'Unit' {
    Context 'Quiet Mode' {
        InModuleScope Install-Identity-Utilities {
            $defaultWebSite = New-Object -TypeName psobject -Property @{Name = "Default Web Site"}
            $testWebSite =  New-Object -TypeName psobject -Property @{Name = "Test Site"}
            Mock -CommandName Get-ChildItem -MockWith { return @( $defaultWebSite, $testWebSite) }
        
            It 'Should return web site given web site name' {
                $selectedSite = Get-IISWebSiteForInstall -selectedSiteName "Default Web Site" -quiet $true
                $selectedSite.Name | Should -Be "Default Web Site"
            }
            It 'Should throw an exception if given site name does not exist' {
                {Get-IISWebSiteForInstall -selectedSiteName "Bad Site" -quiet $true} | Should -Throw
            }
        }
    }

    Context 'Interactive Mode - One Site' {
        InModuleScope Install-Identity-Utilities {
            It 'Should return the only web site when one web site is configured without prompting' {
                $defaultWebSite = New-Object -TypeName psobject -Property @{Name = "Default Web Site"; Id = 1}
                Mock -CommandName Get-ChildItem -MockWith { return $defaultWebSite }
                Mock -CommandName Read-Host -MockWith { }
                $selectedSite = Get-IISWebSiteForInstall -selectedSiteName "" -quiet $false
                $selectedSite.Name | Should -Be "Default Web Site"
                Assert-MockCalled -CommandName Read-Host -Times 0 -Exactly
            }  
        }
    }

    Context 'Interactive Mode - Multiple Sites' {
        InModuleScope Install-Identity-Utilities {
            It 'Should return the selected web site' {
                $defaultWebSite = New-Object -TypeName psobject -Property @{Name = "Default Web Site"; Id = 1}
                $testWebSite =  New-Object -TypeName psobject -Property @{Name = "Test Site"; Id = 2}
                Mock -ModuleName Install-Identity-Utilities -CommandName Get-ChildItem -MockWith { return @( $defaultWebSite, $testWebSite) }
                Mock -ModuleName Install-Identity-Utilities -CommandName Read-Host -MockWith { 2 }
                $selectedSite = Get-IISWebSiteForInstall -selectedSiteName "" -quiet $false
                $selectedSite.Name | Should -Be "Test Site"
            }  
            
            It 'Should throw an exception when an invalid site is selected' {
                $defaultWebSite = New-Object -TypeName psobject -Property @{Name = "Default Web Site"; Id = 1}
                $testWebSite =  New-Object -TypeName psobject -Property @{Name = "Test Site"; Id = 2}
                Mock -ModuleName Install-Identity-Utilities -CommandName Get-ChildItem -MockWith { return @( $defaultWebSite, $testWebSite) }
                Mock -ModuleName Install-Identity-Utilities -CommandName Read-Host -MockWith { 3 }
                {Get-IISWebSiteForInstall -selectedSiteName "" -quiet $false } | Should -Throw
            }
        }
    }
}