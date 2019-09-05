param(
    [string] $targetFilePath = "$PSScriptRoot\..\Install-Identity-Utilities.psm1"
)

Write-Host $targetFilePath
# Force re-import to pick up latest changes
Import-Module $targetFilePath -Force

Describe 'Install-DotNetCoreIfNeeded' -Tag 'Unit' {
    Context 'DotNetCore version is already installed'{
        It 'Should Not Install'{
            Mock -ModuleName Install-Identity-Utilities -CommandName Test-PrerequisiteExact -MockWith { return $true }
            Mock -ModuleName Install-Identity-Utilities -CommandName Get-WebRequestDownload -MockWith {}
            Install-DotNetCoreIfNeeded -version "1.1.1234.123" -downloadUrl "http://example.com"
            Assert-MockCalled -ModuleName Install-Identity-Utilities -CommandName Get-WebRequestDownload -Times 0 -Exactly

        }
        InModuleScope Install-Identity-Utilities {
            It 'Should install proper version' {
                Mock -CommandName Test-PrerequisiteExact -MockWith { return $false }
                Mock -CommandName Get-WebRequestDownload -MockWith {}
                Mock -CommandName Start-Process -MockWith {}
                Mock -CommandName Restart-W3SVC -MockWith {}
                Mock -CommandName Remove-Item -MockWith {}
                Install-DotNetCoreIfNeeded -version "1.1.1234.123" -downloadUrl "http://example.com"
                Assert-MockCalled -CommandName Get-WebRequestDownload -Times 1 -Exactly
                Assert-MockCalled -CommandName Start-Process -Times 1 -Exactly
                Assert-MockCalled -CommandName Restart-W3SVC -Times 1 -Exactly
                Assert-MockCalled -CommandName Remove-Item -Times 1 -Exactly
            }

            It 'Should throw an exception if Start-Process throws' {
                Mock -CommandName Test-PrerequisiteExact -MockWith { return $false }
                Mock -CommandName Get-WebRequestDownload -MockWith {}
                Mock -CommandName Start-Process -MockWith { throw }
                Mock -CommandName Restart-W3SVC -MockWith {}
                Mock -CommandName Remove-Item -MockWith {}
                {Install-DotNetCoreIfNeeded -version "1.1.1234.123" -downloadUrl "http://example.com"} | Should -Throw
            }
        }
    }
}