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
        Context 'Possible Client Scenario' -Tag 'Integration' {
            BeforeAll {
                function Add-InstallationSetting {
                    # Mock to overwrite actual implementation parameter checks
                }

                $testCert = New-Object -TypeName System.Security.Cryptography.X509Certificates.x509Certificate2
                Mock -CommandName Add-InstallationSetting { }
                Mock -CommandName Write-DosMessage { }
            }
            It 'Should replace certificate and installer secret, when certificate is expired' {
                # Arrange
                $certPath = "cert:\CurrentUser\My"
                $newCert = New-SelfSignedCertificate -DnsName "Test Release Pipeline" -FriendlyName "TestCertForPester" -CertStoreLocation $certPath -Provider "Microsoft Strong Cryptographic Provider"

                $fabricInstallerSecret = "!!enc!!:some secret"
                $newSecret = "new secret"

                $identityDbConnectionString = "some connection string"
                $installSettings = @{encryptionCertificateThumbprint = "some thumbprint"}

                Mock -CommandName Get-Certificate {  Mock -CommandName Get-Certificate { return $testCert } }
                Mock -CommandName Test-IdentityEncryptionCertificateValid { return $false }
                Mock -CommandName Remove-IdentityEncryptionCertificate { }
                Mock -CommandName New-IdentityEncryptionCertificate { return $newCert }

                Mock -CommandName  Unprotect-DosInstallerSecret { } # Simulates error occured
                Mock -CommandName Invoke-ResetFabricInstallerSecret { return $newSecret }
  
                # Act
                $encCert = Get-IdentityEncryptionCertificate `
                -installSettings $installSettings `
                -configStorePath $configStorePath `
                -validate
                
                # Assert
                $encCert | Should -eq $newCert
                Assert-MockCalled Write-DosMessage -Times 1 -Scope 'It' -ParameterFilter {
                    $Level -eq "Information" `
                    -and $Message.StartsWith("The provided certificate")
                }
            
                $decryptedSecret = Get-IdentityFabricInstallerSecret `
                -fabricInstallerSecret $fabricInstallerSecret `
                -encryptionCertificateThumbprint $encCert.Thumbprint `
                -identityDbConnectionString $identityDbConnectionString

                # Assert
                $decryptedSecret | Should -eq $newSecret
                Assert-MockCalled Add-InstallationSetting -Times 3 -Scope 'It'
                Assert-MockCalled Unprotect-DosInstallerSecret -Times 1 -Scope 'It'
                Assert-MockCalled Invoke-ResetFabricInstallerSecret -Times 1 -Scope 'It'
                
                Set-Location -Path $certPath
                $encCert | Remove-Item

                Set-Location $PSScriptRoot
            }
        }
    }
    
    AfterAll {
        Remove-Module Install-Identity-Utilities -Force
    }
}