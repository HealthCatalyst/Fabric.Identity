param(
    [string] $targetFilePath = "$PSScriptRoot\..\Install-Identity-Utilities.psm1"
)

Write-Host $targetFilePath
# Force re-import to pick up latest changes
Import-Module $targetFilePath -Force

Describe 'Get-AppInsightsKey'{
    Context 'Quiet Mode'{
        InModuleScope Install-Identity-Utilities{
            It 'Should return stored app insights key'{
                # Arrange
                Mock -ModuleName Install-Identity-Utilities -CommandName Read-Host -MockWith { }
                Mock -ModuleName Install-Identity-Utilities -CommandName Add-InstallationSetting -MockWith {}
                
                # Act
                $appInsightsKey = Get-AppInsightsKey -appInsightsInstrumentationKey "123456" -installConfigPath "install.config" -scope "identity" -quiet $true

                # Assert
                $appInsightsKey | Should -Be "123456"
                Assert-MockCalled -ModuleName Install-Identity-Utilities -CommandName Read-Host -Times 0 -Exactly
                Assert-MockCalled -ModuleName Install-Identity-Utilities -CommandName Add-InstallationSetting -Times 2 -Exactly
            }
        }
    }

    Context 'Interactive Mode'{
        InModuleScope Install-Identity-Utilities{
            It 'Should return user entered app insights key'{
                # Arrange
                Mock -ModuleName Install-Identity-Utilities -CommandName Read-Host -MockWith { return "567890" }
                Mock -ModuleName Install-Identity-Utilities -CommandName Add-InstallationSetting -MockWith {}

                # Act
                $appInsightsKey = Get-AppInsightsKey -appInsightsInstrumentationKey "123456" -installConfigPath "install.config" -scope "identity" -quiet $false

                # Assert
                $appInsightsKey | Should -Be "567890"
                Assert-MockCalled -ModuleName Install-Identity-Utilities -CommandName Read-Host -Times 1 -Exactly
                Assert-MockCalled -ModuleName Install-Identity-Utilities -CommandName Add-InstallationSetting -Times 2 -Exactly
            }
        }
    }
}
