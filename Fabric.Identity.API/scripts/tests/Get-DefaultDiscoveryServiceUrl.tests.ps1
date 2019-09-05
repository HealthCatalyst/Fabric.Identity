param(
    [string] $targetFilePath = "$PSScriptRoot\..\Install-Identity-Utilities.psm1"
)

Write-Host $targetFilePath
# Force re-import to pick up latest changes
Import-Module $targetFilePath -Force

Describe 'Get-DefaultDiscoveryServiceUrl'{
    Context 'Returns stored URL'{
        InModuleScope Install-Identity-Utilities{
            It 'Should return the stored URL as is'{
                # Arrange
                Mock -ModuleName Install-Identity-Utilities -CommandName Get-FullyQualifiedMachineName -MockWith {}

                # Act
                $discoUrl = Get-DefaultDiscoveryServiceUrl -discoUrl "https://host.fabric.local/DiscoveryService/v1"

                # Assert
                $discoUrl | Should -Be "https://host.fabric.local/DiscoveryService/v1"
                Assert-MockCalled -ModuleName Install-Identity-Utilities -CommandName Get-FullyQualifiedMachineName -Times 0 -Exactly
            }
            It 'Should return the stored URL after trimming the trailing slash'{
                # Arrange
                Mock -ModuleName Install-Identity-Utilities -CommandName Get-FullyQualifiedMachineName -MockWith {}

                # Act
                $discoUrl = Get-DefaultDiscoveryServiceUrl -discoUrl "https://host.fabric.local/DiscoveryService/v1/"

                # Assert
                $discoUrl | Should -Be "https://host.fabric.local/DiscoveryService/v1"
                Assert-MockCalled -ModuleName Install-Identity-Utilities -CommandName Get-FullyQualifiedMachineName -Times 0 -Exactly
            }
            It 'Should return the stored URL after appending a v1'{
                # Arrange
                Mock -ModuleName Install-Identity-Utilities -CommandName Get-FullyQualifiedMachineName -MockWith {}

                # Act
                $discoUrl = Get-DefaultDiscoveryServiceUrl -discoUrl "https://host.fabric.local/DiscoveryService/"

                # Assert
                $discoUrl | Should -Be "https://host.fabric.local/DiscoveryService/v1"
                Assert-MockCalled -ModuleName Install-Identity-Utilities -CommandName Get-FullyQualifiedMachineName -Times 0 -Exactly
            }
        }
    }
    Context 'Returns calculated URL'{
        InModuleScope Install-Identity-Utilities{
            It 'Should return the calculated discovery URL'{
                # Arrange
                Mock -ModuleName Install-Identity-Utilities -CommandName Get-FullyQualifiedMachineName -MockWith { "https://host.fabric.local"}

                # Act
                $discoUrl = Get-DefaultDiscoveryServiceUrl -discoUrl $null

                # Assert
                $discoUrl | Should -Be "https://host.fabric.local/DiscoveryService/v1"
                Assert-MockCalled -ModuleName Install-Identity-Utilities -CommandName Get-FullyQualifiedMachineName -Times 1 -Exactly
            }
        }
    }
}
