param(
    [string] $targetFilePath = "$PSScriptRoot\..\Install-Identity-Utilities.psm1"
)

Write-Host $targetFilePath
# Force re-import to pick up latest changes
Import-Module $targetFilePath -Force

Describe 'Test-DiscoveryService Unit Tests'{
    Context 'Success'{
        It 'Should succeed and not throw an exception'{
            # Arrange
            Mock Invoke-RestMethod {} -ModuleName Install-Discovery-Utilities

            # Act/Assert
            {Test-DiscoveryService -discoveryBaseUrl "https://host.domain.local/DiscoveryService" } | Should -Not -Throw
            Assert-MockCalled Invoke-RestMethod -ModuleName Install-Discovery-Utilities -Times 1 -ParameterFilter { $Uri -eq "https://host.domain.local/DiscoveryService/v1/Services" }
        }
    }
    Context 'Generic Exception'{
        It 'Should fail and throw an exception'{
            # Arrange
            Mock Invoke-RestMethod { throw "bad stuff happened" } -ModuleName Install-Discovery-Utilities

            # Act/Assert
            {Test-DiscoveryService -discoveryBaseUrl "https://host.domain.local/DiscoveryService" } | Should -Throw
        }
    }
}