param(
    [string] $targetFilePath = "$PSScriptRoot\..\Install-IdPSS-Utilities.psm1"
)

# Force re-import to pick up latest changes
Import-Module $targetFilePath -Force
$directoryPath = [System.IO.Path]::GetDirectoryName($targetFilePath)
$identityUtilitiesPath = Join-Path -Path $directoryPath -ChildPath "/Install-Identity-Utilities.psm1"
Import-Module $identityUtilitiesPath -Force

Describe 'New-FabricAzureADApplication' -Tag 'Unit' {
    BeforeAll {
        $returnPrincipal = @(
            @{
                ServicePrincipalNames = "https://graph.microsoft.com"
                AppRoles = @(
                    @{
                        Id = 1
                        Value = "Directory.Read.All"
                    },
                    @{
                        Id = 2
                        Value = "User.Read.All"
                    },
                    @{
                        Id = 3
                        Value = "Other.Permission.All"
                    }
                )
            },
            @{
                ServicePrincipalNames = "OtherUrl"
            }
        )
    }
    Context 'New Application' {
        It 'should create a new Azure application' {
            Mock -CommandName Get-AzureADApplication {}
            Mock -CommandName Connect-AzureAD {}
            Mock -CommandName New-AzureADApplication {}
            Mock -CommandName Set-AzureADApplication {}
            Mock -CommandName Get-AzureADServicePrincipal {return $returnPrincipal}

            New-FabricAzureADApplication -appName "app" -replyUrls @(@{name="url"})
            Assert-MockCalled -CommandName New-AzureADApplication -Times 1 -Exactly
            Assert-MockCalled -CommandName Set-AzureADApplication -Times 0 -Exactly
        }
    }
    Context 'Existing Application' {
        It 'should update an existing azure application' {
            $returnApp = @{
                ObjectId = 1234
                ReplyUrls = New-Object System.Collections.Generic.List[string]
            }

            Mock -CommandName Get-AzureADApplication { return $returnApp}
            Mock -CommandName Connect-AzureAD {}
            Mock -CommandName New-AzureADApplication {}
            Mock -CommandName Set-AzureADApplication {}
            Mock -CommandName Get-AzureADServicePrincipal {return $returnPrincipal}

            New-FabricAzureADApplication -appName "app" -replyUrls @(@{name="url"})
            Assert-MockCalled -CommandName New-AzureADApplication -Times 0 -Exactly
            Assert-MockCalled -CommandName Set-AzureADApplication -Times 1 -Exactly
        }
    }
}