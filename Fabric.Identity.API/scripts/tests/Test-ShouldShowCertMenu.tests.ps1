param(
    [string] $targetFilePath = "$PSScriptRoot\..\Install-Identity-Utilities.psm1"
)

Write-Host $targetFilePath
# Force re-import to pick up latest changes
Import-Module $targetFilePath -Force

Describe 'Test-ShouldShowCertMenu'{
    InModuleScope Install-Identity-Utilities{
        Context 'Interactive Mode'{
            It 'Should return true when in interactive mode and signing cert thumbprint is not present'{
                $shouldShow = Test-ShouldShowCertMenu -primarySigningCertificateThumbprint "" -encryptionCertificateThumbprint "12345" -quiet $false
                $shouldShow | Should -Be $true
            }
            It 'Should return true when in interactive mode and encryption cert thumbprint is not present'{
                $shouldShow = Test-ShouldShowCertMenu -primarySigningCertificateThumbprint "12345" -encryptionCertificateThumbprint "" -quiet $false
                $shouldShow | Should -Be $true
            }
            It 'Should return false when in interactive mode and encryption and signing cert thumbprint are present'{
                $shouldShow = Test-ShouldShowCertMenu -primarySigningCertificateThumbprint "12345" -encryptionCertificateThumbprint "12345" -quiet $false
                $shouldShow | Should -Be $false
            }
            It 'Should return true when in interactive mode and no thumbprints are present'{
                $shouldShow = Test-ShouldShowCertMenu -primarySigningCertificateThumbprint "" -encryptionCertificateThumbprint "" -quiet $false
                $shouldShow | Should -Be $true
            }
        }
        Context 'Quiet Mode'{
            It 'Should return false when in quiet mode and signing and encryption cert thumbprnts are present'{
                $shouldShow = Test-ShouldShowCertMenu -primarySigningCertificateThumbprint "12345" -encryptionCertificateThumbprint "12345" -quiet $true
                $shouldShow | Should -Be $false
            }
            It 'Should return false when in quiet mode and signing cert thumbprint is not present'{
                $shouldShow = Test-ShouldShowCertMenu -primarySigningCertificateThumbprint "" -encryptionCertificateThumbprint "12345" -quiet $true
                $shouldShow | Should -Be $false
            }
            It 'Should return false when in quiet mode and encryption cert thumbprint is not present'{
                $shouldShow = Test-ShouldShowCertMenu -primarySigningCertificateThumbprint "12345" -encryptionCertificateThumbprint "" -quiet $true
                $shouldShow | Should -Be $false
            }
            It 'Should return false when in quiet mode and no thumbprints are present'{
                $shouldShow = Test-ShouldShowCertMenu -primarySigningCertificateThumbprint "" -encryptionCertificateThumbprint "" -quiet $true
                $shouldShow | Should -Be $false
            }
        }
    }
}