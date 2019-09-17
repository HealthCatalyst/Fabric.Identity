param(
    [string] $targetFilePath = "$PSScriptRoot\..\Install-IdPSS-Utilities.psm1"
)

Write-Host $targetFilePath
# Force re-import to pick up latest changes
Import-Module $targetFilePath -Force

$Global:testInstallFile = "install.config"
$Global:testInstallFileLoc = "$PSScriptRoot\$testInstallFile"

Describe 'Get-Tenants' -Tag 'Unit' {
    InModuleScope Install-IdPSS-Utilities {
    Context 'Tenants exists in config' {
        It 'Should return a list of tenants' {
            Mock -ModuleName Install-IdPSS-Utilities -CommandName Get-TenantSettingsFromInstallConfig { return @(@{name="tenant1";alias="alias1"}, @{name="tenant2";alias="alias2"})}
            $tenants = Get-Tenants -azureConfigPath $testInstallFileLoc
            $tenants.Count | Should -Be 2
            $tenants[0].name | Should -Be "tenant1"
            $tenants[0].alias | Should -Be "alias1"
            $tenants[1].name | Should -Be "tenant2"
            $tenants[1].alias | Should -Be "alias2"
        }
        It 'Should throw when no tenants in install.config' {
            Mock -ModuleName Install-IdPSS-Utilities -CommandName Get-TenantSettingsFromInstallConfig {}
            { Get-Tenants -azureConfigPath $testInstallFileLoc } | Should -Throw
        }
        It 'Should throw when no tenants alias in install.config' {
            Mock -ModuleName Install-IdPSS-Utilities -CommandName Get-TenantSettingsFromInstallConfig { return @(@{name="tenant1"}, @{name="tenant2"})}
            { Get-Tenants -azureConfigPath $testInstallFileLoc } | Should -Throw
        }
    }
  } 
}