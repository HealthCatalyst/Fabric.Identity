param(
    [string] $targetFilePath = "$PSScriptRoot\..\Install-IdPSS-Utilities.psm1"
)

# Force re-import to pick up latest changes
Import-Module $targetFilePath -Force
$directoryPath = [System.IO.Path]::GetDirectoryName($targetFilePath)
$identityUtilitiesPath = Join-Path -Path $directoryPath -ChildPath "/Install-Identity-Utilities.psm1"
Import-Module $identityUtilitiesPath -Force

$Global:testInstallFile = "install.config"
$Global:testInstallFileLoc = "$PSScriptRoot\$testInstallFile"

Describe 'Get-ReplyUrls' -Tag 'Unit' {
    InModuleScope Install-IdPSS-Utilities {
        Context 'Urls exists in config' {
            It 'Should return a list of urls' {
                Mock -ModuleName Install-IdPSS-Utilities -CommandName Get-TenantSettingsFromInstallConfig { return @("url1", "url2")}
                $urls = Get-ReplyUrls -azureConfigPath $testInstallFileLoc
                $urls.Count | Should -Be 2
                $urls[0] | Should -Be "url1"
                $urls[1] | Should -Be "url2"
            }
        }
        Context 'Urls do not exist in config' {
            InModuleScope Install-IdPSS-Utilities {
                It 'Should throw when no replyUrl in install.config' {
                    Mock -ModuleName Install-IdPSS-Utilities -CommandName Get-TenantSettingsFromInstallConfig {}
                    { Get-ReplyUrls -installConfigPath $testInstallFileLoc } | Should -Throw
                }
            }
        }
    }
}