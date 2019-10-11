param(
    [string] $targetFilePath = "$PSScriptRoot\..\Install-IdPSS-Utilities.psm1"
)

Write-Host $targetFilePath
# Force re-import to pick up latest changes
Import-Module $targetFilePath -Force

 Describe 'Get-CombinedAzureADPermissions' {
    InModuleScope Install-IdPSS-Utilities {
        Context 'Happy Paths' {
            BeforeAll {
                $s1ScopePermission = New-Object -TypeName "Microsoft.Open.AzureAD.Model.ResourceAccess" -ArgumentList "permission-1","Scope"
                $s2ScopePermission = New-Object -TypeName "Microsoft.Open.AzureAD.Model.ResourceAccess" -ArgumentList "permission-2","Scope"
                $r1RolePermission = New-Object -TypeName "Microsoft.Open.AzureAD.Model.ResourceAccess" -ArgumentList "permission-1","Role"
            }
            BeforeEach {
                $existingRequiredResourceAccess = New-Object -TypeName "Microsoft.Open.AzureAD.Model.RequiredResourceAccess"
                $existingRequiredResourceAccess.ResourceAppId = "matching-id"

                $newRequiredResourceAccess = New-Object -TypeName "Microsoft.Open.AzureAD.Model.RequiredResourceAccess"
                $newRequiredResourceAccess.ResourceAppId = "matching-id"
            }
            It 'Should combine permissions' {
                # Arrange
                $existingRequiredResourceAccess.ResourceAccess = $s1ScopePermission
                $newRequiredResourceAccess.ResourceAccess = $s2ScopePermission

                # Act
                $combinedRequiredResourceAccess = Get-CombinedAzureADPermissions `
                    -existingRequiredResourceAccess $existingRequiredResourceAccess `
                    -newRequiredResourceAccess $newRequiredResourceAccess

                # Assert
                $combinedRequiredResourceAccess.ResourceAccess.Count | Should -eq 2
                $combinedRequiredResourceAccess.ResourceAccess | Should -contain $s1ScopePermission
                $combinedRequiredResourceAccess.ResourceAccess | Should -contain $s2ScopePermission
            }
            
            It 'Should ignore duplicate permissions' {
                # Arrange
                $existingRequiredResourceAccess.ResourceAccess = $s1ScopePermission,$s1ScopePermission
                $newRequiredResourceAccess.ResourceAccess = $s2ScopePermission

                # Act
                $combinedPerms = Get-CombinedAzureADPermissions `
                    -existingRequiredResourceAccess $existingRequiredResourceAccess `
                    -newRequiredResourceAccess $newRequiredResourceAccess

                # Assert
                $combinedPerms.ResourceAccess.Count | Should -eq 2
                $combinedPerms.ResourceAccess | Should -contain $s1ScopePermission
                $combinedPerms.ResourceAccess | Should -contain $s2ScopePermission
            }

            It 'Should ignore duplicate permissions when only one permission is passed' {
                # Arrange
                $existingRequiredResourceAccess.ResourceAccess = $s1ScopePermission
                $newRequiredResourceAccess.ResourceAccess = $s1ScopePermission

                # Act
                $combinedPerms = Get-CombinedAzureADPermissions `
                    -existingRequiredResourceAccess $existingRequiredResourceAccess `
                    -newRequiredResourceAccess $newRequiredResourceAccess

                # Assert
                $combinedPerms.ResourceAccess.Count | Should -eq 1
                $combinedPerms.ResourceAccess | Should -contain $s1ScopePermission
            }

            It 'Should uniquely identity scope vs role permissions' {
                # Arrange 
                $existingRequiredResourceAccess.ResourceAccess = $s1ScopePermission
                $newRequiredResourceAccess.ResourceAccess = $r1RolePermission

                # Act
                $combinedPerms = Get-CombinedAzureADPermissions `
                    -existingRequiredResourceAccess $existingRequiredResourceAccess `
                    -newRequiredResourceAccess $newRequiredResourceAccess

                # Assert
                $combinedPerms.ResourceAccess.Count | Should -eq 2
                $combinedPerms.ResourceAccess | Should -contain $s1ScopePermission
                $combinedPerms.ResourceAccess | Should -contain $r1RolePermission
            }

            It 'Should combine only permission from the same resource based off the new permissions being added' {
                $existingNonMatchingRequiredResourceAccess = New-Object -TypeName "Microsoft.Open.AzureAD.Model.RequiredResourceAccess"
                $existingNonMatchingRequiredResourceAccess.ResourceAppId = "nonmatching-id"
                $existingNonMatchingRequiredResourceAccess.ResourceAccess = $s1ScopePermission
                $existingRequiredResourceAccess.ResourceAccess = $s2ScopePermission

                $existingRequiredResourceAccessArray = @()
                $existingRequiredResourceAccessArray += $existingRequiredResourceAccess
                $existingRequiredResourceAccessArray += $existingNonMatchingRequiredResourceAccess

                $newRequiredResourceAccess.ResourceAccess = $r1RolePermission

                # Act
                $combinedPerms = Get-CombinedAzureADPermissions `
                    -existingRequiredResourceAccess $existingRequiredResourceAccessArray `
                    -newRequiredResourceAccess $newRequiredResourceAccess

                # Assert
                $combinedPerms.Count | Should -eq 2 # Verify that existing permissions were not deleted
                $combinedPerms[0].ResourceAccess.Count | Should -eq 2 # Verify the merged permissions were added correctly
                $combinedPerms[0].ResourceAccess | Should -contain $r1RolePermission
                $combinedPerms[0].ResourceAccess | Should -contain $s2ScopePermission
            }
        }
    }

    AfterAll {
        Remove-Module Install-IdPSS-Utilities -Force
    }
}
