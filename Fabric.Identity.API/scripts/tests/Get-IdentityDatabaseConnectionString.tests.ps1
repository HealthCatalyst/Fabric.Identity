param(
    [string] $targetFilePath = "$PSScriptRoot\..\Install-Identity-Utilities.psm1"
)

Write-Host $targetFilePath
# Force re-import to pick up latest changes
Import-Module $targetFilePath -Force

Describe 'Get-IdentityDatabaseConnectionString'{
    Context 'Quiet Mode'{
        InModuleScope Install-Identity-Utilities{
            It 'Should return stored Identity db connection string'{
                # Arrange
                Mock -ModuleName Install-Identity-Utilities -CommandName Invoke-Sql -MockWith {}
                Mock -ModuleName Install-Identity-Utilities -CommandName Add-InstallationSetting -MockWith {}
                Mock -ModuleName Install-Identity-Utilities -CommandName Read-Host -MockWith {}

                # Act
                $dbconnectionString = Get-IdentityDatabaseConnectionString -identityDbName "identity" -sqlServerAddress "host.fabric.local" -installConfigPath "install.config" -quiet $true

                # Assert
                $dbConnectionString.DbName | Should -Be "identity"
                $dbConnectionString.DbConnectionString | Should -Be "Server=host.fabric.local;Database=identity;Trusted_Connection=True;MultipleActiveResultSets=True;"
                Assert-MockCalled -ModuleName Install-Identity-Utilities -CommandName Read-Host -Times 0 -Exactly
                Assert-MockCalled -ModuleName Install-Identity-Utilities -CommandName Invoke-Sql -Times 1 -Exactly
                Assert-MockCalled -ModuleName Install-Identity-Utilities -CommandName Add-InstallationSetting -Times 1 -Exactly
            }

            It 'Should throw when cannot connect to database'{
                # Arrange
                Mock -ModuleName Install-Identity-Utilities -CommandName Invoke-Sql -MockWith { throw }
                Mock -ModuleName Install-Identity-Utilities -CommandName Add-InstallationSetting -MockWith {}
                Mock -ModuleName Install-Identity-Utilities -CommandName Read-Host -MockWith {}

                # Act/Assert
                {Get-IdentityDatabaseConnectionString -identityDbName "identity" -sqlServerAddress "host.fabric.local" -installConfigPath "install.config" -quiet $true } | Should -Throw
            }
        }
    }

    Context 'Interactive Mode'{
        InModuleScope Install-Identity-Utilities{
            It 'Should return user entered Identity db connection string'{
                # Arrange
                Mock -ModuleName Install-Identity-Utilities -CommandName Invoke-Sql -MockWith {}
                Mock -ModuleName Install-Identity-Utilities -CommandName Add-InstallationSetting -MockWith {}
                Mock -ModuleName Install-Identity-Utilities -CommandName Read-Host -MockWith { "identity2" }

                # Act
                $dbconnectionString = Get-IdentityDatabaseConnectionString -identityDbName "identity" -sqlServerAddress "host.fabric.local" -installConfigPath "install.config" -quiet $false

                # Assert
                $dbConnectionString.DbName | Should -Be "identity2"
                $dbConnectionString.DbConnectionString | Should -Be "Server=host.fabric.local;Database=identity2;Trusted_Connection=True;MultipleActiveResultSets=True;"
                Assert-MockCalled -ModuleName Install-Identity-Utilities -CommandName Read-Host -Times 1 -Exactly
                Assert-MockCalled -ModuleName Install-Identity-Utilities -CommandName Invoke-Sql -Times 1 -Exactly
                Assert-MockCalled -ModuleName Install-Identity-Utilities -CommandName Add-InstallationSetting -Times 1 -Exactly
            }
        }
    }
}