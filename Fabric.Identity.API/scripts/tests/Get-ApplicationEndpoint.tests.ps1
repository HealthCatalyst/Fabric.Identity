param(
    [string] $targetFilePath = "$PSScriptRoot\..\Install-Identity-Utilities.psm1"
)

Write-Host $targetFilePath
# Force re-import to pick up latest changes
Import-Module $targetFilePath -Force

$Global:testInstallFile = "install.config"
$Global:testInstallFileLoc = "$PSScriptRoot\$testInstallFile"

Describe 'Get-ApplicationEndpoint'{
    Context 'Quiet Mode'{
        InModuleScope Install-Identity-Utilities{
            It 'Should return stored ApplicationEndpoint'{
                # Arrange
                Mock -ModuleName Install-Identity-Utilities -CommandName Get-FullyQualifiedMachineName -MockWith { }
                Mock -ModuleName Install-Identity-Utilities -CommandName Read-Host -MockWith { }
                Mock -ModuleName Install-Identity-Utilities -CommandName Add-InstallationSetting -MockWith {}
                
                # Act
                $appEndpoint = Get-ApplicationEndpoint -appName "identity" -applicationEndpoint "https://host.fabric.local/identity" -installConfigPath $testInstallFileLoc -scope "identity" -quiet $true

                # Assert
                $appEndpoint | Should -Be "https://host.fabric.local/identity"
                Assert-MockCalled -ModuleName Install-Identity-Utilities -CommandName Read-Host -Times 0 -Exactly
                Assert-MockCalled -ModuleName Install-Identity-Utilities -CommandName Get-FullyQualifiedMachineName -Times 0 -Exactly
                Assert-MockCalled -ModuleName Install-Identity-Utilities -CommandName Add-InstallationSetting -Times 2 -Exactly
            }
        }
    }

    Context 'Interactive Mode'{
        InModuleScope Install-Identity-Utilities{
            It 'Should return user DiscoveryService URL'{
                # Arrange
                Mock -ModuleName Install-Identity-Utilities -CommandName Get-FullyQualifiedMachineName -MockWith { }
                Mock -ModuleName Install-Identity-Utilities -CommandName Read-Host -MockWith { "https://otherhost.fabric.local/identity" }
                Mock -ModuleName Install-Identity-Utilities -CommandName Add-InstallationSetting -MockWith {}
                
                # Act
                $appEndpoint = Get-ApplicationEndpoint -appName "identity" -applicationEndpoint "https://host.fabric.local/identity" -installConfigPath $testInstallFileLoc -scope "identity" -quiet $false

                # Assert
                $appEndpoint | Should -Be "https://otherhost.fabric.local/identity"
                Assert-MockCalled -ModuleName Install-Identity-Utilities -CommandName Read-Host -Times 1 -Exactly
                Assert-MockCalled -ModuleName Install-Identity-Utilities -CommandName Get-FullyQualifiedMachineName -Times 0 -Exactly
                Assert-MockCalled -ModuleName Install-Identity-Utilities -CommandName Add-InstallationSetting -Times 2 -Exactly
            }
        }
    }
}
