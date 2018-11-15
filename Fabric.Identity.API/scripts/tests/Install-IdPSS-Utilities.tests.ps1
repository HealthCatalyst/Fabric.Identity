param(
    [string] $targetFilePath = "$PSScriptRoot\..\Install-IdPSS-Utilities.psm1"
)

# Force re-import to pick up latest changes
Import-Module $targetFilePath -Force

Describe 'Get-FabricAzureADSecret' -Tag 'Unit' {
    Context 'Happy Path' {
        It 'Should create and return a credential' {
            $enc = [system.Text.Encoding]::UTF8
            $mockResp = @{
                CustomKeyIdentifier = $enc.GetBytes("PowerShell Created Password")
                KeyId = "Id"
            }
            $mockObj = @{
                Value = "value"
            }

            Mock -CommandName New-AzureADApplicationPasswordCredential -MockWith { return $mockObj }
            Mock -CommandName Get-AzureADApplicationPasswordCredential -MockWith { return $mockResp }
            Mock -CommandName Remove-AzureADApplicationPasswordCredential -MockWith {}
            Mock -CommandName Write-Host {}

            $value = Get-FabricAzureADSecret -objectId "value"
            Assert-MockCalled -CommandName New-AzureADApplicationPasswordCredential -Times 1 -Exactly
            Assert-MockCalled -CommandName Get-AzureADApplicationPasswordCredential -Times 1 -Exactly
            Assert-MockCalled -CommandName Remove-AzureADApplicationPasswordCredential -Times 1 -Exactly
            $value | Should -Be $mockObj.Value
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
            $mockXml = [xml]'<?xml version="1.0" encoding="utf-8"?><installation><settings><scope name="identity"><variable name="fabricInstallerSecret" value="" /><variable name="discoveryService" value="" />	<registeredApplications><variable tenantId="tenant1" secret="secret1" clientid="clientid1" /><variable tenantId="tenant2" secret="secret2" clientid="clientid2" /></registeredApplications></scope></settings></installation>'

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

Describe 'Get-SettingsFromInstallConfig' -Tag 'Unit' {
    Context 'Section Exists' {
        It 'should return a list of settings' {
            $mockXml = [xml]'<?xml version="1.0" encoding="utf-8"?><installation><settings><scope name="identity"><variable name="fabricInstallerSecret" value="" /><variable name="discoveryService" value="" />	<section><variable name="value1" /><variable name="value2" /></section></scope></settings></installation>'
            Mock -CommandName Get-Content { return $mockXml }
            $results = Get-SettingsFromInstallConfig -installConfigPath $targetFilePath -scope "identity" -setting "section"
            $results.Count | Should -Be 2
        }
    }
    Context 'Section Does not exist' {
        It 'should return nothing' {
            $mockXml = [xml]'<?xml version="1.0" encoding="utf-8"?><installation><settings><scope name="identity"><variable name="fabricInstallerSecret" value="" /><variable name="discoveryService" value="" />	<section><variable name="value1" /><variable name="value2" /></section></scope></settings></installation>'
            Mock -CommandName Get-Content { return $mockXml }
            $results = Get-SettingsFromInstallConfig -installConfigPath $targetFilePath -scope "identity" -setting "invalid"
            $results | Should -Be $null
        }
    }
}

Describe 'Get-Tenants' -Tag 'Unit' {
    Context 'Tenants exists in config' {
        It 'Should return a list of tenants' {
            Mock -ModuleName Install-IdPSS-Utilities -CommandName Get-SettingsFromInstallConfig { return @("tenant1", "tenant2")}
            $tenants = Get-Tenants -installConfigPath $targetFilePath
            $tenants.Count | Should -Be 2
            $tenants[0] | Should -Be "tenant1"
            $tenants[1] | Should -Be "tenant2"
        }
    }
}

Describe 'Get-ReplyUrls' -Tag 'Unit' {
    Context 'Urls exists in config' {
        It 'Should return a list of urls' {
            Mock -ModuleName Install-IdPSS-Utilities -CommandName Get-SettingsFromInstallConfig { return @("url1", "url2")}
            $urls = Get-ReplyUrls -installConfigPath $targetFilePath
            $urls.Count | Should -Be 2
            $urls[0] | Should -Be "url1"
            $urls[1] | Should -Be "url2"
        }
    }
    Context 'Urls do not exist in config' {
        InModuleScope Install-IdPSS-Utilities {
            It 'Should build and add the new replyUrl' {
                Mock Add-NestedSetting {}
                Mock Get-ApplicationEndpoint { return "localhost" }
                Mock -ModuleName Install-IdPSS-Utilities -CommandName Get-SettingsFromInstallConfig {}
                $urls = Get-ReplyUrls -installConfigPath $targetFilePath

                Assert-MockCalled -CommandName Add-NestedSetting -Times 1 -Exactly
                $urls.Count | Should -Be 1
                $urls | Should -Be "localhost"
            }
        }
    }

}