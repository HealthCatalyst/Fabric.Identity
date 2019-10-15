param(
    [string] $targetFilePath = "$PSScriptRoot\..\Install-Identity-Utilities.psm1"
)

 Write-Host $targetFilePath
# Force re-import to pick up latest changes
Import-Module $targetFilePath -Force

 Describe 'Get-IdentityGetRSAPrivateKey' {
    InModuleScope Install-Identity-Utilities {
        It 'Should return an RSA private key' {
            # Arrange
            Mock -CommandName Write-DosMessage {}
            Mock -CommandName Get-IdentityRSAPrivateKeyNetWrapper {}
            $testCert = New-Object -TypeName System.Security.Cryptography.X509Certificates.x509Certificate2

            # Act/Assert
            Get-IdentityRSAPrivateKey -certificate $testCert
            Assert-MockCalled Write-DosMessage -Times 0 -ParameterFilter { $Level -eq "Error" } -Scope 'It'

        }

        It 'Should throw an error when certificate is missing private key' {
            # Arrange
            Mock -CommandName Write-DosMessage {}
            $testCert = New-Object -TypeName System.Security.Cryptography.X509Certificates.x509Certificate2
            Mock -CommandName Get-IdentityRSAPrivateKeyNetWrapper { throw }

            # Act/Assert
            {Get-IdentityRSAPrivateKey -certificate $testCert } | Should -Throw
            Assert-MockCalled Write-DosMessage -Times 1 -Scope 'It' -ParameterFilter {
                $Level -eq "Error" `
                -and $Message.StartsWith("Could not get the RSA private key")
            }
        }
    }
}