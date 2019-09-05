param(
    [string] $targetFilePath = "$PSScriptRoot\..\Install-IdPSS-Utilities.psm1"
)

# Force re-import to pick up latest changes
Import-Module $targetFilePath -Force
$directoryPath = [System.IO.Path]::GetDirectoryName($targetFilePath)
$identityUtilitiesPath = Join-Path -Path $directoryPath -ChildPath "/Install-Identity-Utilities.psm1"
Import-Module $identityUtilitiesPath -Force

Describe 'Get-SettingsFromInstallConfig' -Tag 'Unit' {
    Context 'Section Exists' {
        It 'should return a list of settings' {
            $mockXml = [xml]'<?xml version="1.0" encoding="utf-8"?><installation><settings><scope name="identity"><variable name="fabricInstallerSecret" value="" /><variable name="discoveryService" value="" />	<section><variable name="value1" /><variable name="value2" /></section></scope></settings></installation>'
            Mock -CommandName Get-Content { return $mockXml }
            $results = Get-TenantSettingsFromInstallConfig -installConfigPath $targetFilePath -scope "identity" -setting "section"
            $results.Count | Should -Be 2
        }
    }
    Context 'Section Does not exist' {
        It 'should return nothing' {
            $mockXml = [xml]'<?xml version="1.0" encoding="utf-8"?><installation><settings><scope name="identity"><variable name="fabricInstallerSecret" value="" /><variable name="discoveryService" value="" />	<section><variable name="value1" /><variable name="value2" /></section></scope></settings></installation>'
            Mock -CommandName Get-Content { return $mockXml }
            $results = Get-TenantSettingsFromInstallConfig -installConfigPath $targetFilePath -scope "identity" -setting "invalid"
            $results | Should -Be $null
        }
    }
}