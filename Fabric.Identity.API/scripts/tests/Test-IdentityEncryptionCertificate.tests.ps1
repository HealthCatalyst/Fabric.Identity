param(
    [string] $targetFilePath = "$PSScriptRoot\..\Install-Identity-Utilities.psm1"
)

Write-Host $targetFilePath
# Force re-import to pick up latest changes
Import-Module $targetFilePath -Force

Describe 'Test-IdentityEncryptionCertificate' {
    It 'Should return false when cert is expired' {
        # Arrange
        $testDate = Get-Date
        $testCert = New-MockObject System.Security.Cryptography.X509Certificates.x509Certificate2
        $expirationProperty = @{
            MemberType = [System.Management.Automation.PSMemberTypes]::NoteProperty
            Name = "NotAfter"
            value = $testDate.AddDays(-1)
            Force = $true
        }
        $testCert | Add-Member @expirationProperty -ErrorAction 0

        # Act
        $value = Test-IdentityEncryptionCertificateValid -encryptionCertificate $testCert

        # Assert
        $value | Should -Be $false
    }

    It 'Should return true when cert is not expired' {
        # Arrange
        $testDate = Get-Date
        $testCert = New-MockObject System.Security.Cryptography.X509Certificates.x509Certificate2
        $expirationProperty = @{
            MemberType = [System.Management.Automation.PSMemberTypes]::NoteProperty
            Name = "NotAfter"
            value = $testDate.AddDays(10)
            Force = $true
        }
        $testCert | Add-Member @expirationProperty -ErrorAction 0

        # Act
        $value = Test-IdentityEncryptionCertificateValid -encryptionCertificate $testCert

        # Assert
        $value | Should -Be $true
    }
    AfterAll {
        Remove-Module Install-Identity-Utilities -Force
    }
}

