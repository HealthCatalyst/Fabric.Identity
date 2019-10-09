param(
    [string] $targetFilePath = "$PSScriptRoot\..\Install-IdPSS-Utilities.psm1"
)

Write-Host $targetFilePath
# Force re-import to pick up latest changes
Import-Module $targetFilePath -Force

Describe 'Remove-IdentityClaimsIssuerFromTenantsList' {
    Context 'Happy path' {
        BeforeEach {
            $tenantsArray = @(
                @{
                    name = "name1"
                    alias = "alias1"
                },
                @{
                    name = "name2"
                    alias = "alias2"
                },
                @{
                    name = "name3"
                    alias = "alias3"
                }
            )
        }
        It 'Should remove the claims issuer tenant provided' {
            # Arrange
            $claimsIssuerName = "name2"

            # Act
            $resultTenants += Remove-IdentityClaimsIssuerFromTenantsList -tenants $tenantsArray -claimsIssuerName $claimsIssuerName
            # Assert
            $resultTenants.Count | Should -eq 2
            $resultTenants[0].name | Should -eq "name1"
            $resultTenants[1].name | Should -eq "name3"
        }

        It 'Should remove the claims issuer tenant provided if only the claims issuer exists' {
            $tenants = @(
                @{
                    name = "name1"
                    alias = "alias1"
                }
            )
            # Arrange
            $claimsIssuerName = "name1"

            # Act
            $resultTenants += Remove-IdentityClaimsIssuerFromTenantsList -tenants $tenants -claimsIssuerName $claimsIssuerName
            # Assert
            $resultTenants.Count | Should -eq 0
        }

        It 'Should not remove any tenant if the claims issuer does not exist' {
            # Arrange
            $claimsIssuerName = "anyname"

            # Act
            $resultTenants += Remove-IdentityClaimsIssuerFromTenantsList -tenants $tenantsArray -claimsIssuerName $claimsIssuerName
            # Assert
            $resultTenants.Count | Should -eq 3
            $resultTenants[0].name | Should -eq "name1"
            $resultTenants[1].name | Should -eq "name2"
            $resultTenants[2].name | Should -eq "name3"
        }
    }
}
