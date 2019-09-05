param(
    [string] $targetFilePath = "$PSScriptRoot\..\Install-Identity-Utilities.psm1"
)

Write-Host $targetFilePath
# Force re-import to pick up latest changes
Import-Module $targetFilePath -Force

Describe 'Get-DefaultApplicationEndpoint'{
    Context 'Gets stored app endpoint'{
        InModuleScope Install-Identity-Utilities{
            It 'Should return the stored app endpoint'{
                # Arrange
                Mock -ModuleName Install-Identity-Utilities -CommandName Get-FullyQualifiedMachineName -MockWith {}

                # Act
                $appEndpoint = Get-DefaultApplicationEndpoint -appName "identity" -appEndpoint "https://host.fabric.local/identity"

                # Assert
                $appEndpoint | Should -Be "https://host.fabric.local/identity"
                Assert-MockCalled -ModuleName Install-Identity-Utilities -CommandName Get-FullyQualifiedMachineName -Times 0 -Exactly
            }
        }
    }
    Context 'Gets calculated app endpoint'{
        InModuleScope Install-Identity-Utilities{
            It 'Should return the calculated app endpoint'{
                # Arrange
                Mock -ModuleName Install-Identity-Utilities -CommandName Get-FullyQualifiedMachineName -MockWith { "https://host.fabric.local" }

                # Act
                $appEndpoint = Get-DefaultApplicationEndpoint -appName "identity" -appEndpoint $null

                # Assert
                $appEndpoint | Should -Be "https://host.fabric.local/identity"
                Assert-MockCalled -ModuleName Install-Identity-Utilities -CommandName Get-FullyQualifiedMachineName -Times 1 -Exactly
            }
        }
    }
}
