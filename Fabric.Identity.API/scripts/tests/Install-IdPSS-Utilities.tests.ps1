param(
    [string] $targetFilePath = "$PSScriptRoot\..\Install-IdPSS-Utilities.psm1"
)

# Force re-import to pick up latest changes
Import-Module $targetFilePath -Force

Describe 'IdPSS Unit Tests' {
Describe 'Get-FabricAzureADSecret' -Tag 'Unit' {
    Context 'Happy Path' {
        InModuleScope Install-IdPSS-Utilities {
            Mock -CommandName Get-InstallIdPSSUtilsUserConfirmation -MockWith { return $true }
            It 'Should create and return a credential' {
                $enc = [system.Text.Encoding]::UTF8
                $mockResp = @{
                    CustomKeyIdentifier = $enc.GetBytes("Non Existing")
                    KeyId = "Id"
                }
                $mockObj = @{
                    Value = "value"
                }

                Mock -CommandName New-AzureADApplicationPasswordCredential -MockWith { return $mockObj }
                Mock -CommandName Get-AzureADApplicationPasswordCredential -MockWith { return $mockResp }
                Mock -CommandName Remove-AzureADApplicationPasswordCredential -MockWith {}
                Mock -CommandName Write-Host {}

                $value = Get-FabricAzureADSecret -objectId "value" -secretName "New Secret"
                Assert-MockCalled -CommandName New-AzureADApplicationPasswordCredential -Scope It -Times 1 -Exactly
                Assert-MockCalled -CommandName Get-AzureADApplicationPasswordCredential -Scope It -Times 1 -Exactly
                Assert-MockCalled -CommandName Remove-AzureADApplicationPasswordCredential -Scope It -Times 0 -Exactly
                $value | Should -Be $mockObj.Value
            }

            It 'Should delete existing/create and return a credential' {
                $enc = [system.Text.Encoding]::UTF8
                $mockResp = @{
                    CustomKeyIdentifier = $enc.GetBytes("Existing Secret")
                    KeyId = "Id"
                }
                $mockObj = @{
                    Value = "value"
                }

                Mock -ModuleName Install-IdPSS-Utilities -CommandName New-AzureADApplicationPasswordCredential -MockWith { return $mockObj }
                Mock -ModuleName Install-IdPSS-Utilities -CommandName Get-AzureADApplicationPasswordCredential -MockWith { return $mockResp }
                Mock -ModuleName Install-IdPSS-Utilities -CommandName Remove-AzureADApplicationPasswordCredential -MockWith {}
                Mock -ModuleName Install-IdPSS-Utilities -CommandName Write-Host {}

                $value = Get-FabricAzureADSecret -objectId "value" -secretName "Existing Secret"
                Assert-MockCalled -CommandName New-AzureADApplicationPasswordCredential -Scope It -Times 1 -Exactly
                Assert-MockCalled -CommandName Get-AzureADApplicationPasswordCredential -Scope It -Times 1 -Exactly
                Assert-MockCalled -CommandName Remove-AzureADApplicationPasswordCredential -Scope It -Times 1 -Exactly
                $value | Should -Be $mockObj.Value
            }
        }
    }

    Context 'Azure AD Errors Creating Secrets' {
        InModuleScope Install-IdPSS-Utilities {
        It 'Should retry before failing when creating a secret' {
            $enc = [system.Text.Encoding]::UTF8
            $mockResp = @{
                CustomKeyIdentifier = $enc.GetBytes("PowerShell Created Password")
                KeyId = "Id"
            }

            Mock -CommandName New-AzureADApplicationPasswordCredential -MockWith { throw }
            Mock -CommandName Get-AzureADApplicationPasswordCredential -MockWith { return $mockResp }
            Mock -CommandName Remove-AzureADApplicationPasswordCredential -MockWith {}
            Mock -CommandName Get-AzureADApplicationPasswordCredential -MockWith {}
            Mock -CommandName Start-Sleep {}
            Mock -CommandName Write-DosMessage {}
            Mock -CommandName Write-Host {}

            { Get-FabricAzureADSecret -objectId "value" } | Should -Throw
            Assert-MockCalled -CommandName Write-DosMessage -ParameterFilter { $Level -and $Level -eq "Error" } -Times 1 -Exactly
            Assert-MockCalled -CommandName Write-DosMessage -ParameterFilter { $Level -and $Level -eq "Warning" } -Times 4 -Exactly
        }
      }
    }
    Context 'Azure AD Errors Removing Secrets' {
        InModuleScope Install-IdPSS-Utilities {
            Mock -CommandName Get-InstallIdPSSUtilsUserConfirmation -MockWith { return $true }
            It 'Should retry before failing when removing a secret' {
                $enc = [system.Text.Encoding]::UTF8
                $mockResp = @{
                    CustomKeyIdentifier = $enc.GetBytes("PowerShell Created Password")
                    KeyId = "Id"
                }

                Mock -ModuleName Install-IdPSS-Utilities -CommandName Get-AzureADApplicationPasswordCredential -MockWith { return $mockResp }
                Mock -ModuleName Install-IdPSS-Utilities -CommandName Remove-AzureADApplicationPasswordCredential -MockWith { throw }
                Mock -ModuleName Install-IdPSS-Utilities -CommandName New-AzureADApplicationPasswordCredential {}
                Mock -ModuleName Install-IdPSS-Utilities -CommandName Start-Sleep {}
                Mock -ModuleName Install-IdPSS-Utilities -CommandName Write-DosMessage {} -ParameterFilter { $Level -and $Level -eq "Warning" }

                { Get-FabricAzureADSecret -objectId "value" -secretName "PowerShell Created Password" } | Should -Throw
                Assert-MockCalled -CommandName Write-DosMessage -ParameterFilter { $Level -and $Level -eq "Warning" } -Times 4 -Exactly
            }
        }
    }
}

Describe 'Connect-AzureADTenant' -Tag 'Unit' {
    BeforeAll {
        $password = ConvertTo-SecureString "SecretPassword" -AsPlainText -Force
        $credentials = New-Object System.Management.Automation.PSCredential ("username", $password)
    }
    InModuleScope Install-IdPSS-Utilities {
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
}

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
    InModuleScope Install-IdPSS-Utilities {
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
}

Describe 'Get-SettingsFromInstallConfig' -Tag 'Unit' {
    InModuleScope Install-Identity-Utilities {
    Context 'Section Exists' {
        It 'should return a list of settings' {
            $mockXml = [xml]'<?xml version="1.0" encoding="utf-8"?><installation><settings><scope name="identity"><variable name="fabricInstallerSecret" value="" /><variable name="discoveryService" value="" />	<section><variable name="value1" /><variable name="value2" /></section></scope></settings></installation>'
            Mock -CommandName Get-Content { return $mockXml }
            $results = Get-TenantSettingsFromInstallConfig -installConfigPath "install.config" -scope "identity" -setting "section"
            $results.Count | Should -Be 2
        }
    }
    Context 'Section Does not exist' {
        It 'should return nothing' {
            $mockXml = [xml]'<?xml version="1.0" encoding="utf-8"?><installation><settings><scope name="identity"><variable name="fabricInstallerSecret" value="" /><variable name="discoveryService" value="" />	<section><variable name="value1" /><variable name="value2" /></section></scope></settings></installation>'
            Mock -CommandName Get-Content { return $mockXml }
            $results = Get-TenantSettingsFromInstallConfig -installConfigPath "install.config" -scope "identity" -setting "invalid"
            $results | Should -Be $null
        }
    }
  }
}

Describe 'Get-Tenants' -Tag 'Unit' {
    InModuleScope Install-IdPSS-Utilities {
    Context 'Tenants exists in config' {
        It 'Should return a list of tenants' {
            Mock -ModuleName Install-IdPSS-Utilities -CommandName Get-TenantSettingsFromInstallConfig { return @(@{name="tenant1";alias="alias1"}, @{name="tenant2";alias="alias2"})}
            $tenants = Get-Tenants -azureConfigPath "install.config"
            $tenants.Count | Should -Be 2
            $tenants[0].name | Should -Be "tenant1"
            $tenants[0].alias | Should -Be "alias1"
            $tenants[1].name | Should -Be "tenant2"
            $tenants[1].alias | Should -Be "alias2"
        }
        It 'Should throw when no tenants in install.config' {
            Mock -ModuleName Install-IdPSS-Utilities -CommandName Get-TenantSettingsFromInstallConfig {}
            { Get-Tenants -azureConfigPath "install.config" } | Should -Throw
        }
        It 'Should throw when no tenants alias in install.config' {
            Mock -ModuleName Install-IdPSS-Utilities -CommandName Get-TenantSettingsFromInstallConfig { return @(@{name="tenant1"}, @{name="tenant2"})}
            { Get-Tenants -azureConfigPath "install.config" } | Should -Throw
        }
    }
  } 
}

Describe 'Get-ReplyUrls' -Tag 'Unit' {
    InModuleScope Install-IdPSS-Utilities {
    Context 'Urls exists in config' {
        It 'Should return a list of urls' {
            Mock -ModuleName Install-IdPSS-Utilities -CommandName Get-TenantSettingsFromInstallConfig { return @("url1", "url2")}
            $urls = Get-ReplyUrls -azureConfigPath "install.config"
            $urls.Count | Should -Be 2
            $urls[0] | Should -Be "url1"
            $urls[1] | Should -Be "url2"
        }
    }
    Context 'Urls do not exist in config' {
        InModuleScope Install-IdPSS-Utilities {
            It 'Should throw when no replyUrl in install.config' {
                Mock -ModuleName Install-IdPSS-Utilities -CommandName Get-TenantSettingsFromInstallConfig {}
                { Get-ReplyUrls -installConfigPath "install.config" } | Should -Throw
            }
        }
    }
   }
  }
}
