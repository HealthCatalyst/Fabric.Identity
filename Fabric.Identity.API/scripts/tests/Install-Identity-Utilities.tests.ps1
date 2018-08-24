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
            Mock -CommandName Test-Path -MockWith { return $true}
            $path = Get-FullyQualifiedInstallationZipFile -zipPackage "somefile.zip" -workingDirectory $PSScriptRoot
            $path | Should -Be "$PSScriptRoot\somefile.zip"
        }

        It 'Should return full path specified when full path is specified as a parameter'{
            Mock -CommandName Test-Path -MockWith { return $true }
            $expectedPath = "$env:TEMP\somfile.zip"
            $path = Get-FullyQualifiedInstallationZipFile -zipPackage $expectedPath -workingDirectory $PSScriptRoot
            $path | Should -Be $expectedPath
        }
    }

    Context 'Zip File Does Not Exist'{
        It 'Should throw an exception if the zip file does not exist'{
            Mock -CommandName Test-Path -MockWith {return $false}
            {Get-FullyQualifiedInstallationZipFile -zipPackage "somefile.zip" -workingDirectory $PSScriptRoot} | Should -Throw
        }
    }
}

Describe 'Get-IISWebSiteForInstall' -Tag 'Unit' {
    Context 'Quiet Mode' {
        BeforeAll {
            $defaultWebSite = New-Object -TypeName psobject -Property @{Name = "Default Web Site"}
            $testWebSite =  New-Object -TypeName psobject -Property @{Name = "Test Site"}
            Mock -CommandName Get-ChildItem -MockWith { return @( $defaultWebSite, $testWebSite) }
        }
        It 'Should return web site given web site name' {
            $selectedSite = Get-IISWebSiteForInstall -selectedSiteName "Default Web Site" -quiet $true
            $selectedSite.Name | Should -Be "Default Web Site"
        }
        It 'Should throw an exception if given site name does not exist' {
            {Get-IISWebSiteForInstall -selectedSiteName "Bad Site" -quiet $true} | Should -Throw
        }
    }

    Context 'Interactive Mode' {
        It 'Should return the only web site when one web site is configured' {
            $defaultWebSite = New-Object -TypeName psobject -Property @{Name = "Default Web Site"; Id = 1}
            Mock -CommandName Get-ChildItem -MockWith { return $defaultWebSite }
            Mock -CommandName Read-Host -MockWith { }
            $selectedSite = Get-IISWebSiteForInstall -selectedSiteName "Default Web Site" -quiet $false
            $selectedSite.Name | Should -Be "Default Web Site"
            Assert-MockCalled -CommandName Read-Host -Times 0 -Exactly
        }

        It 'Should return the selected web site' {
            $defaultWebSite = New-Object -TypeName psobject -Property @{Name = "Default Web Site"; Id = 1}
            $testWebSite =  New-Object -TypeName psobject -Property @{Name = "Test Site"; Id = 2}
            Mock -CommandName Get-ChildItem -MockWith { return @( $defaultWebSite, $testWebSite) }
            Mock -CommandName Read-Host -MockWith { 2 }
            $selectedSite = Get-IISWebSiteForInstall -selectedSiteName "Default Web Site" -quiet $false
            $selectedSite.Name | Should -Be "Test Site"
        }       
    }
}