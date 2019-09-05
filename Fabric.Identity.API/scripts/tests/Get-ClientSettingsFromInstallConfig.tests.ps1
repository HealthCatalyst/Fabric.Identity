param(
    [string] $targetFilePath = "$PSScriptRoot\..\Install-Identity-Utilities.psm1"
)

Write-Host $targetFilePath
# Force re-import to pick up latest changes
Import-Module $targetFilePath -Force

Describe 'Get-ClientSettingsFromInstallConfig' -Tag 'Unit' {
    Context 'Valid config path' {
        It 'should return a list of client settings' {
            $mockXml = [xml]'<?xml version="1.0" encoding="utf-8"?><installation><settings><scope name="identity"><variable name="fabricInstallerSecret" value="" /><variable name="discoveryService" value="" />	<registeredApplications><variable appName="testApp" tenantId="tenant1" secret="secret1" clientid="clientid1" /><variable appName="testApp" tenantId="tenant2" secret="secret2" clientid="clientid2" /></registeredApplications></scope></settings></installation>'

            Mock -CommandName Get-Content { return $mockXml }
            $result = Get-ClientSettingsFromInstallConfig -installConfigPath $targetFilePath -appName "testApp"
            $result.length | Should -Be 2
            $firstApp = $result[0]
            $secondApp = $result[1]

            $firstApp.clientId | Should -Be "clientid1"
            $firstApp.tenantId | Should -Be "tenant1"
            $firstApp.clientSecret | Should -Be "secret1"

            $secondApp.clientId | Should -Be "clientid2"
            $secondApp.tenantId | Should -Be "tenant2"
            $secondApp.clientSecret | Should -Be "secret2"
        }
    }
}