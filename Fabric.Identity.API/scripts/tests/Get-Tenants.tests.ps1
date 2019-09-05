param(
    [string] $targetFilePath = "$PSScriptRoot\..\Install-IdPSS-Utilities.psm1"
)

# Force re-import to pick up latest changes
Import-Module $targetFilePath -Force
$directoryPath = [System.IO.Path]::GetDirectoryName($targetFilePath)
$identityUtilitiesPath = Join-Path -Path $directoryPath -ChildPath "/Install-Identity-Utilities.psm1"
Import-Module $identityUtilitiesPath -Force

Describe 'Get-Tenants' -Tag 'Unit' {
    Context 'Tenants exists in config' {
        It 'Should return a list of tenants' {
            Mock -ModuleName Install-IdPSS-Utilities -CommandName Get-TenantSettingsFromInstallConfig { return @(@{name="tenant1";alias="alias1"}, @{name="tenant2";alias="alias2"})}
            $tenants = Get-Tenants -installConfigPath $targetFilePath
            $tenants.Count | Should -Be 2
            $tenants[0].name | Should -Be "tenant1"
            $tenants[0].alias | Should -Be "alias1"
            $tenants[1].name | Should -Be "tenant2"
            $tenants[1].alias | Should -Be "alias2"
        }
        It 'Should throw when no tenants in install.config' {
            Mock -ModuleName Install-IdPSS-Utilities -CommandName Get-TenantSettingsFromInstallConfig {}
            { Get-Tenants -installConfigPath $targetFilePath } | Should -Throw
        }
        It 'Should throw when no tenants alias in install.config' {
            Mock -ModuleName Install-IdPSS-Utilities -CommandName Get-TenantSettingsFromInstallConfig { return @(@{name="tenant1"}, @{name="tenant2"})}
            { Get-Tenants -installConfigPath $targetFilePath } | Should -Throw
        }
    }
}