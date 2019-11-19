param(
    [string] $targetFilePath = "$PSScriptRoot\..\Install-Identity-Utilities.psm1"
)

Write-Host $targetFilePath
# Force re-import to pick up latest changes
Import-Module $targetFilePath -Force

Describe 'New-IdentityEncryptionCertificate ' {
    InModuleScope Install-Identity-Utilities {
        It 'Should return a CNG certificate' {
            # Arrange
            $testCert = New-Object -TypeName System.Security.Cryptography.X509Certificates.x509Certificate2
            $testSubject = "Test Cert"
            $location = "Cert:\CurrentUser\My"
            Mock New-SelfSignedCertificate { return $testCert }

            # Act
            $certificate = New-IdentityEncryptionCertificate -subject $testSubject -certStoreLocation $location

            # Assert
            Assert-MockCalled -ModuleName Install-Identity-Utilities `
                -CommandName New-SelfSignedCertificate `
                -ParameterFilter {
                    $Subject -eq $testSubject `
                    -and $CertStoreLocation -eq $location 
                }
            $certificate | Should -Be $testCert
        }
    }

    InModuleScope Install-Identity-Utilities {
        It 'Should return a CNG certificate with inferred Subject' {
            # Arrange
            $testCert = New-Object -TypeName System.Security.Cryptography.X509Certificates.x509Certificate2
            $location = "Cert:\LocalMachine\My"
            Mock New-SelfSignedCertificate { return $testCert }
            $testSubject = "$env:computername.$((Get-WmiObject Win32_ComputerSystem).Domain.tolower())"

            # Act
            $certificate = New-IdentityEncryptionCertificate -certStoreLocation $location

            # Assert
            Assert-MockCalled -ModuleName Install-Identity-Utilities `
                -CommandName New-SelfSignedCertificate `
                -ParameterFilter {
                    $Subject -eq $testSubject `
                    -and $CertStoreLocation -eq $location
                }
            $certificate | Should -Be $testCert
        }
    }

    AfterAll {
        Remove-Module Install-Identity-Utilities -Force
    }
}
