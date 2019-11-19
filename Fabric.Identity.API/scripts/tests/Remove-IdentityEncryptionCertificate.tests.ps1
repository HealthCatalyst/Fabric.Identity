param(
    [string] $targetFilePath = "$PSScriptRoot\..\Install-Identity-Utilities.psm1"
)

Write-Host $targetFilePath
# Force re-import to pick up latest changes
Import-Module $targetFilePath -Force

Describe 'Test-IdentityEncryptionCertificate' {
    
    InModuleScope Install-Identity-Utilities {
        Context 'Happy path' {
            InModuleScope Install-Identity-Utilities {
                It 'Should remove certificate' {
                    # Arrange
                    $testCert = New-Object -TypeName System.Security.Cryptography.X509Certificates.x509Certificate2
                    $friendlyNameProperty = @{
                        MemberType = [System.Management.Automation.PSMemberTypes]::NoteProperty
                        Name = "FriendlyName"
                        value = "Fabric Identity Signing Encryption Certificate"
                        Force = $true
                    }
                    $testCert | Add-Member @friendlyNameProperty -ErrorAction 0
                    $thumbPrint = "non-existing-thumbprint"
                    Mock -ModuleName Install-Identity-Utilities -CommandName Get-Certificate -MockWith { return $testCert }
                    Mock -ModuleName Install-Identity-Utilities -CommandName Write-DosMessage -MockWith {}
                    Mock -ModuleName Install-Identity-Utilities -CommandName Remove-Item -MockWith {}
        
                    # Act
                    Remove-IdentityEncryptionCertificate -encryptionCertificateThumbprint $thumbPrint
        
                    # Assert
                    Assert-MockCalled -ModuleName Install-Identity-Utilities -CommandName Write-DosMessage -ParameterFilter { $Level -and $Level -eq "Information" -and $Message.StartsWith("Removing Identity encryption certificate") } -Times 1 -Exactly -Scope It
                    Assert-MockCalled -ModuleName Install-Identity-Utilities -CommandName Write-DosMessage -Times 1 -Exactly -Scope It
                }
            }
        }

        Context 'Should not remove Certificate' {
            It 'Should not error and should not attempt to remove when certificate is not found' {
                # Arrange
                $thumbPrint = "non-existing-thumbprint"
                Mock -ModuleName Install-Identity-Utilities -CommandName Write-DosMessage -MockWith {}
                Mock -ModuleName Install-Identity-Utilities -CommandName Remove-Item -MockWith {}

                # Act
                Remove-IdentityEncryptionCertificate -encryptionCertificateThumbprint $thumbPrint

                # Assert
                Assert-MockCalled -ModuleName Install-Identity-Utilities -CommandName Write-DosMessage -Times 1 -Exactly -Scope It
                Assert-MockCalled -ModuleName Install-Identity-Utilities -CommandName Write-DosMessage -ParameterFilter { $Level -and $Level -eq "Information" -and $Message.StartsWith("Certificate with thumbprint '$thumbPrint' was not found") } -Times 1 -Exactly -Scope It
                Assert-MockCalled -ModuleName Install-Identity-Utilities -CommandName Remove-Item -Times 0 -Exactly -Scope It
            }

            It 'Should not attempt to remove when certificate is not the identity encryption certificate' {
                # Arrange
                $testCert = New-Object -TypeName System.Security.Cryptography.X509Certificates.x509Certificate2
                $friendlyNameProperty = @{
                    MemberType = [System.Management.Automation.PSMemberTypes]::NoteProperty
                    Name = "FriendlyName"
                    value = "Some Friendly Name"
                    Force = $true
                }
                $testCert | Add-Member @friendlyNameProperty -ErrorAction 0
                $thumbPrint = "non-existing-thumbprint"

                Mock -ModuleName Install-Identity-Utilities -CommandName Get-Certificate -MockWith { return $testCert }
                Mock -ModuleName Install-Identity-Utilities -CommandName Write-DosMessage -MockWith {}
                Mock -ModuleName Install-Identity-Utilities -CommandName Remove-Item -MockWith {}

                # Act
                Remove-IdentityEncryptionCertificate -encryptionCertificateThumbprint $thumbPrint

                # Assert
                Assert-MockCalled -ModuleName Install-Identity-Utilities -CommandName Remove-Item -Times 0 -Exactly -Scope It
                Assert-MockCalled -ModuleName Install-Identity-Utilities -CommandName Write-DosMessage -Times 0 -Exactly -Scope It
            }

            It 'Should not remove if no friendly name exists on certificate' {
                # Arrange
                $testCert = New-Object -TypeName System.Security.Cryptography.X509Certificates.x509Certificate2
                $thumbPrint = "non-existing-thumbprint"

                Mock -ModuleName Install-Identity-Utilities -CommandName Get-Certificate -MockWith { return $testCert }
                Mock -ModuleName Install-Identity-Utilities -CommandName Write-DosMessage -MockWith {}
                Mock -ModuleName Install-Identity-Utilities -CommandName Remove-Item -MockWith {}

                # Act
                Remove-IdentityEncryptionCertificate -encryptionCertificateThumbprint $thumbPrint

                # Assert
                Assert-MockCalled -ModuleName Install-Identity-Utilities -CommandName Write-DosMessage -Times 0 -Exactly -Scope It
                Assert-MockCalled -ModuleName Install-Identity-Utilities -CommandName Remove-Item -Times 0 -Exactly -Scope It
            }
        }
    }

    AfterAll {
        Remove-Module Install-Identity-Utilities -Force
    }
}
