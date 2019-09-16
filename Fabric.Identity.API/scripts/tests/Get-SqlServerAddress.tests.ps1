param(
    [string] $targetFilePath = "$PSScriptRoot\..\Install-Identity-Utilities.psm1"
)

Write-Host $targetFilePath
# Force re-import to pick up latest changes
Import-Module $targetFilePath -Force

$Global:testInstallFile = "install.config"
$Global:testInstallFileLoc = "$PSScriptRoot\$testInstallFile"

Describe 'Get-SqlServerAddress'{
    Context 'Quiet Mode'{
        InModuleScope Install-Identity-Utilities{
            It 'Should return stored sql server address'{
                # Arrange
                Mock -ModuleName Install-Identity-Utilities -CommandName Read-Host -MockWith { }
                Mock -ModuleName Install-Identity-Utilities -CommandName Add-InstallationSetting -MockWith {}
                
                # Act
                $sqlServerAddress = Get-SqlServerAddress -sqlServerAddress "somemachine.fabric.local" -installConfigPath $testInstallFileLoc -quiet $true

                # Assert
                $sqlServerAddress | Should -Be "somemachine.fabric.local"
                Assert-MockCalled -ModuleName Install-Identity-Utilities -CommandName Read-Host -Times 0 -Exactly
                Assert-MockCalled -ModuleName Install-Identity-Utilities -CommandName Add-InstallationSetting -Times 1 -Exactly
            }
        }
    }

    Context 'Interactive Mode'{
        InModuleScope Install-Identity-Utilities{
            It 'Should return user entered sql server address'{
                # Arrange
                Mock -ModuleName Install-Identity-Utilities -CommandName Read-Host -MockWith { return "othermachine.fabric.local" }
                Mock -ModuleName Install-Identity-Utilities -CommandName Add-InstallationSetting -MockWith {}

                # Act
                $sqlServerAddress = Get-SqlServerAddress -sqlServerAddress "somemachine.fabric.local" -installConfigPath $testInstallFileLoc -quiet $false

                # Assert
                $sqlServerAddress | Should -Be "othermachine.fabric.local"
                Assert-MockCalled -ModuleName Install-Identity-Utilities -CommandName Read-Host -Times 1 -Exactly
                Assert-MockCalled -ModuleName Install-Identity-Utilities -CommandName Add-InstallationSetting -Times 1 -Exactly
            }
        }
    }
}
