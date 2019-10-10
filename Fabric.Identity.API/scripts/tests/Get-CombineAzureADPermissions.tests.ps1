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
            It 'Should combine permissions' {
                # Act
                $combinedPerms = Get-CombinedAzureADPermissions `
                    -existingPermissions $s1ScopePermission `
                    -newPermissions $s2ScopePermission

                # Assert
                $combinedPerms.Count | Should -eq 2
                $combinedPerms | Should -contain $s1ScopePermission
                $combinedPerms | Should -contain $s2ScopePermission
            }
            
            It 'Should ignore duplicate permissions' {
                # Arrange
                $existingPerms = @($s1ScopePermission, $s1ScopePermission)
                $newPermissions = $s2ScopePermission

                # Act
                $combinedPerms = Get-CombinedAzureADPermissions `
                    -existingPermissions $existingPerms `
                    -newPermissions $newPermissions

                # Assert
                $combinedPerms.Count | Should -eq 2
                $combinedPerms | Should -contain $s1ScopePermission
                $combinedPerms | Should -contain $newPermissions
            }

            It 'Should ignore duplicate permissions when only one permission is passed' {
                # Arrange
                $existingPerms = $s1ScopePermission
                $newPermissions = $s1ScopePermission

                # Act
                $combinedPerms = Get-CombinedAzureADPermissions `
                    -existingPermissions $existingPerms `
                    -newPermissions $newPermissions

                # Assert
                $combinedPerms.Count | Should -eq 1
                $combinedPerms | Should -contain $s1ScopePermission
            }

            It 'Should uniquely identity scope vs role permissions' {
                # Act
                $combinedPerms = Get-CombinedAzureADPermissions `
                    -existingPermissions $s1ScopePermission `
                    -newPermissions $r1RolePermission

                # Assert
                $combinedPerms.Count | Should -eq 2
                $combinedPerms | Should -contain $s1ScopePermission
                $combinedPerms | Should -contain $r1RolePermission
            }
        }
    }
}
