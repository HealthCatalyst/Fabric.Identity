param(
    [string] $targetFilePath = "$PSScriptRoot\..\Install-Identity-Utilities.psm1"
)

Write-Host $targetFilePath
# Force re-import to pick up latest changes
Import-Module $targetFilePath -Force

 Describe 'Get-IdentityFabricInstallerSecret' {
    InModuleScope Install-Identity-Utilities {
        Context 'Happy Paths' {
            BeforeAll {
                $identityDbConnectionString = "some connection string"
                $encryptionCertificateThumbprint = "some thumbprint"
            }

            It 'Should decrypt and return the installer secret' {
                # Arrange
                $fabricInstallerSecret = "!!enc!!:some secret"
                Mock -CommandName  Unprotect-DosInstallerSecret { return $fabricInstallerSecret }

                # Act
                $decryptedSecret = Get-IdentityFabricInstallerSecret `
                    -fabricInstallerSecret $fabricInstallerSecret `
                    -encryptionCertificateThumbprint $encryptionCertificateThumbprint `
                    -identityDbConnectionString $identityDbConnectionString

                # Assert
                $decryptedSecret | Should -eq $fabricInstallerSecret
            }

            It 'Should not decrypt the installer secret when not encrypted' {
                # Arrange
                $fabricInstallerSecret = "some secret"
                Mock -CommandName  Unprotect-DosInstallerSecret { return $fabricInstallerSecret }

                # Act
                $decryptedSecret = Get-IdentityFabricInstallerSecret `
                    -fabricInstallerSecret $fabricInstallerSecret `
                    -encryptionCertificateThumbprint $encryptionCertificateThumbprint `
                    -identityDbConnectionString $identityDbConnectionString

                # Assert
                $decryptedSecret | Should -eq $fabricInstallerSecret
                Assert-MockCalled Unprotect-DosInstallerSecret -Times 0 -Scope 'It'
            }
        }
        Context 'Unhappy Paths' {
            BeforeAll {
                $identityDbConnectionString = "some connection string"
                $encryptionCertificateThumbprint = "some thumbprint"
            }

            It 'Should create a new secret if no secret was returned' {
                # Arrange
                $fabricInstallerSecret = "!!enc!!:some secret"
                $newSecret = "new secret"
                Mock -CommandName  Unprotect-DosInstallerSecret { } # Simulates error occured
                Mock -CommandName Invoke-ResetFabricInstallerSecret { return $newSecret }

                # Act
                $decryptedSecret = Get-IdentityFabricInstallerSecret `
                    -fabricInstallerSecret $fabricInstallerSecret `
                    -encryptionCertificateThumbprint $encryptionCertificateThumbprint `
                    -identityDbConnectionString $identityDbConnectionString

                # Assert
                $decryptedSecret | Should -eq $newSecret
            }
        }
    }
}