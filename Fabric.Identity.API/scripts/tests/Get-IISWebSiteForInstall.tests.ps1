param(
    [string] $targetFilePath = "$PSScriptRoot\..\Install-Identity-Utilities.psm1"
)

Write-Host $targetFilePath
# Force re-import to pick up latest changes
Import-Module $targetFilePath -Force

$Global:testInstallFile = "install.config"
$Global:testInstallFileLoc = "$PSScriptRoot\$testInstallFile"

Describe 'Get-IISWebSiteForInstall' -Tag 'Unit' {
    Context 'Quiet Mode' {
        InModuleScope Install-Identity-Utilities {
            $defaultWebSite = New-Object -TypeName psobject -Property @{Name = "Default Web Site"}
            $testWebSite =  New-Object -TypeName psobject -Property @{Name = "Test Site"}
            Mock -CommandName Get-ChildItem -MockWith { return @( $defaultWebSite, $testWebSite) }
        
            It 'Should return web site given web site name' {
                $selectedSite = Get-IISWebSiteForInstall -selectedSiteName "Default Web Site" -installConfigPath $testInstallFileLoc -scope "identity" -quiet $true
                $selectedSite.Name | Should -Be "Default Web Site"
            }
            It 'Should throw an exception if given site name does not exist' {
                {Get-IISWebSiteForInstall -selectedSiteName "Bad Site" -installConfigPath $testInstallFileLoc -scope "identity" -quiet $true} | Should -Throw
            }
        }
    }

    Context 'Interactive Mode - One Site' {
        InModuleScope Install-Identity-Utilities {
            It 'Should return the only web site when one web site is configured without prompting' {
                $defaultWebSite = New-Object -TypeName psobject -Property @{Name = "Default Web Site"; Id = 1}
                Mock -CommandName Get-ChildItem -MockWith { return $defaultWebSite }
                Mock -CommandName Read-Host -MockWith { }
                $selectedSite = Get-IISWebSiteForInstall -selectedSiteName "" -installConfigPath $testInstallFileLoc -scope "identity" -quiet $false
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
                $selectedSite = Get-IISWebSiteForInstall -selectedSiteName "" -installConfigPath $testInstallFileLoc -scope "identity" -quiet $false
                $selectedSite.Name | Should -Be "Test Site"
            }  
            
            It 'Should throw an exception when an invalid site is selected' {
                $defaultWebSite = New-Object -TypeName psobject -Property @{Name = "Default Web Site"; Id = 1}
                $testWebSite =  New-Object -TypeName psobject -Property @{Name = "Test Site"; Id = 2}
                Mock -ModuleName Install-Identity-Utilities -CommandName Get-ChildItem -MockWith { return @( $defaultWebSite, $testWebSite) }
                Mock -ModuleName Install-Identity-Utilities -CommandName Read-Host -MockWith { 3 }
                Mock -ModuleName Install-Identity-Utilities -CommandName Write-DosMessage -MockWith { } -ParameterFilter { $Level -and $Level -eq "Information" -and $Message.StartsWith("You must select a web site by id between 1 and") }

                {Get-IISWebSiteForInstall -selectedSiteName "" -installConfigPath $testInstallFileLoc -scope "identity" -quiet $false } | Should -Throw
                Assert-MockCalled -ModuleName Install-Identity-Utilities -CommandName Write-DosMessage -ParameterFilter { $Level -and $Level -eq "Information" -and $Message.StartsWith("You must select a web site by id between 1 and") } -Times 10 -Exactly
            }
        }
    }
}
