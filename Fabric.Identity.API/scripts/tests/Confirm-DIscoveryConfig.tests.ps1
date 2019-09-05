param(
    [string] $targetFilePath = "$PSScriptRoot\..\Install-Identity-Utilities.psm1"
)

Write-Host $targetFilePath
# Force re-import to pick up latest changes
Import-Module $targetFilePath -Force

Describe 'Confirm-DiscoveryConfig Unit Tests'{
    InModuleScope Install-Discovery-Utilities{
        It 'Should validate valid discovery config settings'{
            # Arrange
            $discoveryConfig = @{ appName = "DiscoveryService"; appPoolName = "DiscoveryService"; siteName = "Default Web Site"}
            $commonConfig = @{ sqlServerAddress = "localhost"; metadataDbName = "EDWAdmin"; webServerDomain = "host.domain.local"; clientEnvironment = "dev" }

            # Act/Assert
            { Confirm-DiscoveryConfig -discoveryConfig $discoveryConfig -commonConfig $commonConfig } | Should -Not -Throw
        }
        It 'Should throw if there is an invalid setting'{
            # Arrange
            $discoveryConfig = @{}
            $commonConfig = @{}

            # Act/Assert
            { Confirm-DiscoveryConfig -discoveryConfig $discoveryConfig -commonConfig $commonConfig } | Should -Throw
        }
    }
}