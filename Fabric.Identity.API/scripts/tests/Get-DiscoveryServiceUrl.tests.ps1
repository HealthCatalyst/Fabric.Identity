param(
    [string] $targetFilePath = "$PSScriptRoot\..\Install-Identity-Utilities.psm1"
)

Write-Host $targetFilePath
# Force re-import to pick up latest changes
Import-Module $targetFilePath -Force

Describe 'Get-DiscoveryServiceUrl'{
    Context 'Quiet Mode'{
        InModuleScope Install-Identity-Utilities{
            It 'Should return stored DiscoveryService URL'{
                # Arrange
                Mock -ModuleName Install-Identity-Utilities -CommandName Get-FullyQualifiedMachineName -MockWith { }
                Mock -ModuleName Install-Identity-Utilities -CommandName Read-Host -MockWith { }
                Mock -ModuleName Install-Identity-Utilities -CommandName Add-InstallationSetting -MockWith {}
                
                # Act
                $discoUrl = Get-DiscoveryServiceUrl -discoveryServiceUrl "https://host.fabric.local/DiscoveryService/v1" -installConfigPath "install.config" -quiet $true

                # Assert
                $discoUrl | Should -Be "https://host.fabric.local/DiscoveryService/v1"
                Assert-MockCalled -ModuleName Install-Identity-Utilities -CommandName Read-Host -Times 0 -Exactly
                Assert-MockCalled -ModuleName Install-Identity-Utilities -CommandName Get-FullyQualifiedMachineName -Times 0 -Exactly
                Assert-MockCalled -ModuleName Install-Identity-Utilities -CommandName Add-InstallationSetting -Times 1 -Exactly
            }
        }
    }

    Context 'Interactive Mode'{
        InModuleScope Install-Identity-Utilities{
            It 'Should return user DiscoveryService URL'{
                # Arrange
                Mock -ModuleName Install-Identity-Utilities -CommandName Get-FullyQualifiedMachineName -MockWith { }
                Mock -ModuleName Install-Identity-Utilities -CommandName Read-Host -MockWith { "https://otherhost.fabric.local/DiscoveryService/v1" }
                Mock -ModuleName Install-Identity-Utilities -CommandName Add-InstallationSetting -MockWith {}
                
                # Act
                $discoUrl = Get-DiscoveryServiceUrl -discoveryServiceUrl "https://host.fabric.local/DiscoveryService/v1" -installConfigPath "install.config" -quiet $false

                # Assert
                $discoUrl | Should -Be "https://otherhost.fabric.local/DiscoveryService/v1"
                Assert-MockCalled -ModuleName Install-Identity-Utilities -CommandName Read-Host -Times 1 -Exactly
                Assert-MockCalled -ModuleName Install-Identity-Utilities -CommandName Get-FullyQualifiedMachineName -Times 0 -Exactly
                Assert-MockCalled -ModuleName Install-Identity-Utilities -CommandName Add-InstallationSetting -Times 1 -Exactly
            }
        }
    }
}
