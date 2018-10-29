param(
    [string] $targetFilePath = "$PSScriptRoot\..\Install-IdPSS-Utilities.psm1"
)

# Force re-import to pick up latest changes
Import-Module $targetFilePath -Force

Describe 'Get-FabricAzureADSecret' -Tag 'Unit' {
    Context 'Credential Exists' {
        It 'Should return a credential' {
            Mock -CommandName Get-AzureADApplicationPasswordCredential -MockWith {return $true}
            Mock -CommandName New-AzureADApplicationPasswordCredential -MockWith {}

            $value = Get-FabricAzureADSecret -objectId "value"
            Assert-MockCalled -CommandName New-AzureADApplicationPasswordCredential -Times 0 -Exactly
        }
    }

    Context 'Credential Does not Exist' {
        It 'Should create and return a credential' {
            Mock -CommandName Get-AzureADApplicationPasswordCredential -MockWith {}
            Mock -CommandName New-AzureADApplicationPasswordCredential -MockWith {}

            Get-FabricAzureADSecret -objectId "value"
            Assert-MockCalled -CommandName New-AzureADApplicationPasswordCredential -Times 1 -Exactly
        }
    }
}

Describe 'Connect-AzureADTenant' -Tag 'Unit' {
    BeforeAll {
        $password = ConvertTo-SecureString "SecretPassword" -AsPlainText -Force
        $credentials = New-Object System.Management.Automation.PSCredential ("username", $password)
    }
    Context 'Valid Credentials' {
        Mock -CommandName Connect-AzureAD {}

        Connect-AzureADTenant -credentials $credentials -tenantId "tenant"
    }
    Context 'Invalid Credentials' {
        Mock -CommandName Connect-AzureAD -MockWith { throw }
        Mock -CommandName Write-DosMessage -MockWith { } -ParameterFilter { $Level -and $Level -eq "Error" -and $Message.StartsWith("Could not sign into tenant") }
        {Connect-AzureADTenant -credentials $credentials -tenantId "tenant" } | Should -Throw
        Assert-MockCalled -CommandName Write-DosMessage -ParameterFilter { $Level -and $Level -eq "Error" -and $Message.StartsWith("Could not sign into tenant") } -Times 1 -Exactly
    }
}

Describe 'New-FabricAzureADApplication' -Tag 'Unit' {
    Context 'New Application' {
        $returnPrincipal = @(
            @{
                ServicePrincipalNames = @("https://graph.microsoft.com")
                Oauth2Permissions = @(
                    @{
                        Id = 1
                        Value = "Group.Read.All"
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
                ServicePrincipalNames = @("OtherUrl")
            }
        )

        Mock -CommandName Get-AzureADApplication { return $null}
        Mock -CommandName Connect-AzureAD {}
        Mock -CommandName New-AzureADApplication { return $null}
        Mock -CommandName Set-AzureADApplication {return $null}
        Mock -CommandName Get-AzureADServicePrincipal {return $returnPrincipal}

        New-FabricAzureADApplication -appName "app" -replyUrls @("url")
        Assert-MockCalled -CommandName New-AzureADApplication -Times 1 -Exactly
        Assert-MockCalled -CommandName Set-AzureADApplication -Times 0 -Exactly
    }
    Context 'Existing Application' {
        $returnApp = @{
            ObjectId = 1234
        }
        $returnPrincipal = @(
            @{
                ServicePrincipalNames = @("https://graph.microsoft.com")
                Oauth2Permissions = @(
                    @{
                        Id = 1
                        Value = "Group.Read.All"
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
                ServicePrincipalNames = @("OtherUrl")
            }
        )

        Mock -CommandName Get-AzureADApplication { return $returnApp}
        Mock -CommandName Connect-AzureAD {}
        Mock -CommandName New-AzureADApplication {}
        Mock -CommandName Set-AzureADApplication {return $null}
        Mock -CommandName Get-AzureADServicePrincipal {return $returnPrincipal}

        New-FabricAzureADApplication -appName "app" -replyUrls @("url")
        Assert-MockCalled -CommandName New-AzureADApplication -Times 0 -Exactly
        Assert-MockCalled -CommandName Set-AzureADApplication -Times 1 -Exactly
    }
}

# Describe 'Add-InstallationConfigSetting' -Tag 'Unit' {
#     Context '' {
        
#     }
# }

# Describe 'Get-InstallationConfig' -Tag 'Unit' {
#     Context '' {
        
#     }
# }