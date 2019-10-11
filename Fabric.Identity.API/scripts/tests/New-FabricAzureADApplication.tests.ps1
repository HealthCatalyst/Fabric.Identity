param(
    [string] $targetFilePath = "$PSScriptRoot\..\Install-IdPSS-Utilities.psm1"
)

Write-Host $targetFilePath
# Force re-import to pick up latest changes
Import-Module $targetFilePath -Force

Describe 'New-FabricAzureADApplication' -Tag 'Unit' {
    InModuleScope Install-IdPSS-Utilities {
        Context 'Happy Paths' {
            BeforeAll {
                $existingResourceAccess = [Microsoft.Open.AzureAD.Model.ResourceAccess]@{
                    Id = "existing-id"
                    Type = "Role"
                }
                $newResourceAccess = [Microsoft.Open.AzureAD.Model.ResourceAccess]@{
                    Id = "new-id"
                    Type = "Role"
                }
                $emptyRequiredResourceAccessList = @()
            }
            It 'should create a new Azure application' {
                Mock -CommandName Get-AzureADApplication {}
                Mock -CommandName Connect-AzureAD {}
                Mock -CommandName New-AzureADApplication {}
                Mock -CommandName Set-AzureADApplication {}
                Mock -CommandName Get-AzureADServicePrincipal {return $returnPrincipal}

                New-FabricAzureADApplication -appName "app" -replyUrls @(@{name="url"})
                Assert-MockCalled -CommandName New-AzureADApplication -Times 1 -Exactly -Scope It
                Assert-MockCalled -CommandName Set-AzureADApplication -Times 0 -Exactly -Scope It
            }
            It 'should update an existing azure application' {
                # Arrange
                $requiredResourceAccess = New-Object -TypeName "Microsoft.Open.AzureAD.Model.RequiredResourceAccess"
                $requiredResourceAccess.ResourceAccess = $existingResourceAccess,$newResourceAccess
                $returnApp = @{
                    ObjectId = 1234
                    ReplyUrls = New-Object System.Collections.Generic.List[string]
                    RequiredResourceAccess = $requiredResourceAccess
                }

                Mock -CommandName Get-AzureADApplication { return $returnApp }
                Mock -CommandName Connect-AzureAD {}
                Mock -CommandName New-AzureADApplication {}
                Mock -CommandName Set-AzureADApplication {}
                Mock -CommandName Get-AzureADServicePrincipal {return $returnPrincipal}

                # Act
                $app = New-FabricAzureADApplication -appName "app" -replyUrls @(@{name="url"}) -permissions $requiredResourceAccess

                # Assert
                Assert-MockCalled -CommandName New-AzureADApplication -Times 0 -Exactly -Scope It
                Assert-MockCalled -CommandName Set-AzureADApplication -Times 1 -Exactly -Scope It
                $app.RequiredResourceAccess.ResourceAccess.Count | Should -eq 2
                $app.RequiredResourceAccess.ResourceAccess | Should -contain $existingResourceAccess
                $app.RequiredResourceAccess.ResourceAccess | Should -contain $newResourceAccess
            }
        }
    }
    
    AfterAll {
        Remove-Module Install-IdPSS-Utilities -Force
    }
}