param(
    [string] $targetFilePath = "$PSScriptRoot\..\Install-IdPSS-Utilities.psm1"
)

Write-Host $targetFilePath
# Force re-import to pick up latest changes
Import-Module $targetFilePath -Force

Describe 'Get-FabricAzureADSecret' -Tag 'Unit' {
    Context 'Happy Path' {
        InModuleScope Install-IdPSS-Utilities {
            Mock -CommandName Get-InstallIdPSSUtilsUserConfirmation -MockWith { return $true }
            It 'Should create and return a credential' {
                $enc = [system.Text.Encoding]::UTF8
                $mockResp = @{
                    CustomKeyIdentifier = $enc.GetBytes("Non Existing")
                    KeyId = "Id"
                }
                $mockObj = @{
                    Value = "value"
                }

                Mock -CommandName New-AzureADApplicationPasswordCredential -MockWith { return $mockObj }
                Mock -CommandName Get-AzureADApplicationPasswordCredential -MockWith { return $mockResp }
                Mock -CommandName Remove-AzureADApplicationPasswordCredential -MockWith {}
                Mock -CommandName Write-Host {}

                $value = Get-FabricAzureADSecret -objectId "value" -secretName "New Secret"
                Assert-MockCalled -CommandName New-AzureADApplicationPasswordCredential -Scope It -Times 1 -Exactly
                Assert-MockCalled -CommandName Get-AzureADApplicationPasswordCredential -Scope It -Times 1 -Exactly
                Assert-MockCalled -CommandName Remove-AzureADApplicationPasswordCredential -Scope It -Times 0 -Exactly
                $value | Should -Be $mockObj.Value
            }

            It 'Should delete existing/create and return a credential' {
                $enc = [system.Text.Encoding]::UTF8
                $mockResp = @{
                    CustomKeyIdentifier = $enc.GetBytes("Existing Secret")
                    KeyId = "Id"
                }
                $mockObj = @{
                    Value = "value"
                }

                Mock -ModuleName Install-IdPSS-Utilities -CommandName New-AzureADApplicationPasswordCredential -MockWith { return $mockObj }
                Mock -ModuleName Install-IdPSS-Utilities -CommandName Get-AzureADApplicationPasswordCredential -MockWith { return $mockResp }
                Mock -ModuleName Install-IdPSS-Utilities -CommandName Remove-AzureADApplicationPasswordCredential -MockWith {}
                Mock -ModuleName Install-IdPSS-Utilities -CommandName Write-Host {}

                $value = Get-FabricAzureADSecret -objectId "value" -secretName "Existing Secret"
                Assert-MockCalled -CommandName New-AzureADApplicationPasswordCredential -Scope It -Times 1 -Exactly
                Assert-MockCalled -CommandName Get-AzureADApplicationPasswordCredential -Scope It -Times 1 -Exactly
                Assert-MockCalled -CommandName Remove-AzureADApplicationPasswordCredential -Scope It -Times 1 -Exactly
                $value | Should -Be $mockObj.Value
            }
        }
    }

    Context 'Azure AD Errors Creating Secrets' {
        InModuleScope Install-IdPSS-Utilities {
        It 'Should retry before failing when creating a secret' {
            $enc = [system.Text.Encoding]::UTF8
            $mockResp = @{
                CustomKeyIdentifier = $enc.GetBytes("PowerShell Created Password")
                KeyId = "Id"
            }

            Mock -CommandName New-AzureADApplicationPasswordCredential -MockWith { throw }
            Mock -CommandName Get-AzureADApplicationPasswordCredential -MockWith { return $mockResp }
            Mock -CommandName Remove-AzureADApplicationPasswordCredential -MockWith {}
            Mock -CommandName Get-AzureADApplicationPasswordCredential -MockWith {}
            Mock -CommandName Start-Sleep {}
            Mock -CommandName Write-DosMessage {}
            Mock -CommandName Write-Host {}

            { Get-FabricAzureADSecret -objectId "value" } | Should -Throw
            Assert-MockCalled -CommandName Write-DosMessage -ParameterFilter { $Level -and $Level -eq "Error" } -Times 1 -Exactly
            Assert-MockCalled -CommandName Write-DosMessage -ParameterFilter { $Level -and $Level -eq "Warning" } -Times 4 -Exactly
        }
      }
    }
    Context 'Azure AD Errors Removing Secrets' {
        InModuleScope Install-IdPSS-Utilities {
            Mock -CommandName Get-InstallIdPSSUtilsUserConfirmation -MockWith { return $true }
            It 'Should retry before failing when removing a secret' {
                $enc = [system.Text.Encoding]::UTF8
                $mockResp = @{
                    CustomKeyIdentifier = $enc.GetBytes("PowerShell Created Password")
                    KeyId = "Id"
                }

                Mock -ModuleName Install-IdPSS-Utilities -CommandName Get-AzureADApplicationPasswordCredential -MockWith { return $mockResp }
                Mock -ModuleName Install-IdPSS-Utilities -CommandName Remove-AzureADApplicationPasswordCredential -MockWith { throw }
                Mock -ModuleName Install-IdPSS-Utilities -CommandName New-AzureADApplicationPasswordCredential {}
                Mock -ModuleName Install-IdPSS-Utilities -CommandName Start-Sleep {}
                Mock -ModuleName Install-IdPSS-Utilities -CommandName Write-DosMessage {} -ParameterFilter { $Level -and $Level -eq "Warning" }

                { Get-FabricAzureADSecret -objectId "value" -secretName "PowerShell Created Password" } | Should -Throw
                Assert-MockCalled -CommandName Write-DosMessage -ParameterFilter { $Level -and $Level -eq "Warning" } -Times 4 -Exactly
            }
        }
    }
}
