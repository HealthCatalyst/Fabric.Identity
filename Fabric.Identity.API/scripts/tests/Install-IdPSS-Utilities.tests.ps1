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
        It 'should connect correctly' {
            Mock -CommandName Connect-AzureAD {}
            Connect-AzureADTenant -credential $credentials -tenantId "tenant"
        }
    }
    Context 'Invalid Credentials' {
        It 'should throw an exception' {
            Mock -CommandName Connect-AzureAD -MockWith { throw }
            Mock -CommandName Write-DosMessage -MockWith { } -ParameterFilter { $Level -and $Level -eq "Error" -and $Message.StartsWith("Could not sign into tenant") }
            {Connect-AzureADTenant -credential $credentials -tenantId "tenant" } | Should -Throw
            Assert-MockCalled -CommandName Write-DosMessage -ParameterFilter { $Level -and $Level -eq "Error" -and $Message.StartsWith("Could not sign into tenant") } -Times 1 -Exactly
        }
    }
}

Describe 'New-FabricAzureADApplication' -Tag 'Unit' {
    BeforeAll {
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
    }
    Context 'New Application' {
        It 'should create a new Azure application' {
            Mock -CommandName Get-AzureADApplication {}
            Mock -CommandName Connect-AzureAD {}
            Mock -CommandName New-AzureADApplication {}
            Mock -CommandName Set-AzureADApplication {}
            Mock -CommandName Get-AzureADServicePrincipal {return $returnPrincipal}

            New-FabricAzureADApplication -appName "app" -replyUrls @("url")
            Assert-MockCalled -CommandName New-AzureADApplication -Times 1 -Exactly
            Assert-MockCalled -CommandName Set-AzureADApplication -Times 0 -Exactly
        }
    }
    Context 'Existing Application' {
        It 'should update an existing azure application' {
            $returnApp = @{
                ObjectId = 1234
            }

            Mock -CommandName Get-AzureADApplication { return $returnApp}
            Mock -CommandName Connect-AzureAD {}
            Mock -CommandName New-AzureADApplication {}
            Mock -CommandName Set-AzureADApplication {}
            Mock -CommandName Get-AzureADServicePrincipal {return $returnPrincipal}

            New-FabricAzureADApplication -appName "app" -replyUrls @("url")
            Assert-MockCalled -CommandName New-AzureADApplication -Times 0 -Exactly
            Assert-MockCalled -CommandName Set-AzureADApplication -Times 1 -Exactly
        }
    }
}

Describe 'Get-ClientSettingsFromInstallConfig' -Tag 'Unit' {
    Context 'Valid config path' {
        It 'should return a list of client settings' {
            $mockXml = [xml]'<?xml version="1.0" encoding="utf-8"?><installation><settings><scope name="common"><variable name="fabricInstallerSecret" value="" /><variable name="discoveryService" value="" />	<tenants><variable tenantId="tenant1" secret="secret1" clientid="clientid1" /><variable tenantId="tenant2" secret="secret2" clientid="clientid2" /></tenants></scope><scope name="identity"></scope></settings></installation>'

            Mock -CommandName Get-Content { return $mockXml }
            $result = Get-ClientSettingsFromInstallConfig -installConfigPath $targetFilePath
            $result.length | Should -Be 2
            $firstApp = $result[0]
            $secondApp = $result[1]

            $firstApp.clientId | Should -Be "clientid1"
            $firstApp.tenantId | Should -Be "tenant1"
            $firstApp.clientSecret | Should -Be "secret1"

            $secondApp.clientId | Should -Be "clientid2"
            $secondApp.tenantId | Should -Be "tenant2"
            $secondApp.clientSecret | Should -Be "secret2"
        }
    }
}