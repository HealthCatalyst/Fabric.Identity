param(
    [string] $targetFilePath = "$PSScriptRoot\..\Install-Identity-Utilities.psm1"
)

Write-Host $targetFilePath
# Force re-import to pick up latest changes
Import-Module $targetFilePath -Force

 Describe 'Get-IdentityEncryptionCertificate' {
    InModuleScope Install-Identity-Utilities {
        Context 'Happy Paths' {
            BeforeAll {
                function Add-InstallationSetting {
                    # Mock to overwrite actual implementation parameter checks
                }

                $testCert = New-Object -TypeName System.Security.Cryptography.X509Certificates.x509Certificate2
                Mock -CommandName Add-InstallationSetting { }
                Mock -CommandName Write-DosMessage { }
            }

            It 'Should return an encryption certificate' {
                # Arrange
                Mock -CommandName Get-Certificate { return $testCert }
                $installSettings = @{encryptionCertificateThumbprint = "some value"}
                $configStorePath = "path"

                # Act
                $encCert =Get-IdentityEncryptionCertificate -installSettings $installSettings -configStorePath $configStorePath

                # Assert
                $encCert | Should -eq $testCert
                Assert-MockCalled Write-DosMessage -Times 0 -Scope 'It' -ParameterFilter {
                    $Level -eq "Information" `
                    -and $Message.StartsWith("Error locating the provided certificate")
                }
                Assert-MockCalled Write-DosMessage -Times 0 -Scope 'It' -ParameterFilter {
                    $Level -eq "Information" `
                    -and $Message.StartsWith("The provided certificate")
                }
                Assert-MockCalled Add-InstallationSetting -Times 3 -Scope 'It'
            }

            It 'Should validate the invalid certificate when validate flag is passed' {
                # Arrange
                Mock -CommandName Get-Certificate { return $testCert }
                Mock -CommandName Test-IdentityEncryptionCertificateValid { return $false }
                Mock -CommandName New-IdentityEncryptionCertificate { return $testCert }
                $installSettings = @{encryptionCertificateThumbprint = "some value"}
                $configStorePath = "path"
                
                # Act
                $encCert = Get-IdentityEncryptionCertificate -installSettings $installSettings -configStorePath $configStorePath -validate

                # Assert
                $encCert | Should -eq $testCert
                Assert-MockCalled Write-DosMessage -Times 0 -Scope 'It' -ParameterFilter {
                    $Level -eq "Information" `
                    -and $Message.StartsWith("Error locating the provided certificate")
                }
                Assert-MockCalled Test-IdentityEncryptionCertificateValid -Times 1 -Scope 'It'
                Assert-MockCalled Write-DosMessage -Times 1 -Scope 'It' -ParameterFilter {
                    $Level -eq "Information" `
                    -and $Message.StartsWith("The provided certificate")
                }
                Assert-MockCalled Add-InstallationSetting -Times 3 -Scope 'It'
            }

            It 'Should create a new certificate when no thumbprint is in the install.config' {
                # Arrange
                Mock -CommandName New-IdentityEncryptionCertificate { return $testCert }
                $installSettings = @{encryptionCertificateThumbprint = ""}

                # Act
                $newCert = Get-IdentityEncryptionCertificate Get-IdentityEncryptionCertificate -installSettings $installSettings -configStorePath $configStorePath

                # Assert
                Assert-MockCalled New-IdentityEncryptionCertificate -Times 1 -Scope 'It'
                Assert-MockCalled Add-InstallationSetting -Times 3 -Scope 'It'
                $newCert | Should -Be $testCert
            }
        }

        Context 'Unhappy paths' {
            BeforeAll {
                function Add-InstallationSetting {
                    # Mock to overwrite actual implementation parameter checks
                }

                $testCert = New-Object -TypeName System.Security.Cryptography.X509Certificates.x509Certificate2
                Mock -CommandName Add-InstallationSetting { }
                Mock -CommandName Write-DosMessage { }
            }
            It 'Should log when an exception is thrown grabbing a certificate, and still return a certificate' {
                # Arrange
                Mock -CommandName Get-Certificate { throw }
                Mock -CommandName Remove-IdentityEncryptionCertificate { }
                Mock -CommandName New-IdentityEncryptionCertificate { return $testCert }
                $installSettings = @{encryptionCertificateThumbprint = "some value"}
    
                # Act
                $encCert = Get-IdentityEncryptionCertificate -installSettings $installSettings -configStorePath $configStorePath

                # Assert
                $encCert | Should -eq $testCert
                Assert-MockCalled Write-DosMessage -Times 1 -Scope 'It' -ParameterFilter {
                    $Level -eq "Information" `
                    -and $Message.StartsWith("Error locating the provided certificate")
                }
                Assert-MockCalled Add-InstallationSetting -Times 3 -Scope 'It'
            }
        }
    }
    
    AfterAll {
        Remove-Module Install-IdPSS-Utilities -Force
    }
}