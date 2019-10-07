param(
    [string] $targetFilePath = "$PSScriptRoot\..\Install-Identity-Utilities.psm1"
)

Write-Host $targetFilePath
# Force re-import to pick up latest changes
Import-Module $targetFilePath -Force

 Describe 'Get-IdentityServiceIdPSSAzureSettings' {
    InModuleScope Install-Identity-Utilities {
        Context 'Happy Paths' {
            BeforeAll {
                function Add-InstallationTenantSettings {
                    # Mock to overwrite actual implementation parameter checks
                }
                Mock -CommandName  Add-InstallationTenantSettings {}
                Mock -CommandName  Get-EncryptedString {}

                # Mock variables
                $expectedDefaultScope = "https://graph.microsoft.com/.default"
                $appName = "some name"
                $testpath = "some path"
                $emptyClientSettings = New-Object System.Collections.Generic.List[HashTable]
            }
            BeforeEach {
                $validClientSettings = New-Object System.Collections.Generic.List[HashTable]
                $validClientSetting = @{
                    clientId = "someclient"
                    tenantId = "sometenant"
                    tenantAlias = "somealias"
                    #secret = "some secret"
                }
                $validClientSettings.Add($validClientSetting)
            }

            It 'Should encrypt a secret when unencrypted and return a hashtable of azure appsettings' {
                # Arrange
                $fabricInstallerSecret = "some secret"
                $encryptedInstallerSecret = "!!enc!!:$fabricInstallerSecret"
                Mock -CommandName  Get-EncryptedString { return $encryptedInstallerSecret }
                
                $validClientSettings[0].Add("clientSecret", $fabricInstallerSecret)

                # Act
                $appSettings = Get-IdentityServiceIdPSSAzureSettings `
                    -clientSettings $validClientSettings `
                    -encryptionCert $testCert `
                    -azureSettingsConfigPath $testPath `
                    -appName $appName

                # Assert
                Assert-MockCalled Get-EncryptedString -Times 1 -Exactly -Scope It
                Assert-MockCalled Add-InstallationTenantSettings -Times 1 -Exactly -Scope It
                $appSettings.Keys.Count | Should -eq 7
                $appSettings."AzureActiveDirectoryClientSettings__Authority" | Should -eq "https://login.microsoftonline.com/"
                $appSettings."AzureActiveDirectoryClientSettings__TokenEndpoint" | Should -eq "/oauth2/v2.0/token"

                $appSettings."AzureActiveDirectoryClientSettings__ClientAppSettings__0__ClientId" | Should -eq $validClientSetting.clientId
                $appSettings."AzureActiveDirectoryClientSettings__ClientAppSettings__0__TenantId" | Should -eq $validClientSetting.tenantId
                $appSettings."AzureActiveDirectoryClientSettings__ClientAppSettings__0__TenantAlias" | Should -eq $validClientSetting.tenantAlias
                $appSettings."AzureActiveDirectoryClientSettings__ClientAppSettings__0__ClientSecret" | Should -eq $encryptedInstallerSecret
                $appSettings."AzureActiveDirectoryClientSettings__ClientAppSettings__0__Scopes__0" | Should -eq $expectedDefaultScope
            }

            It 'Should return a hashtable of azure appsettings' {
                # Arrange
                $fabricInstallerSecret = "!!enc!!:some secret"
                
                $validClientSettings[0].Add("clientSecret", $fabricInstallerSecret)

                # Act
                $appSettings = Get-IdentityServiceIdPSSAzureSettings `
                    -clientSettings $validClientSettings `
                    -encryptionCert $testCert `
                    -azureSettingsConfigPath $testPath `
                    -appName $appName

                # Assert
                $appSettings.Keys.Count | Should -eq 7
                $appSettings."AzureActiveDirectoryClientSettings__Authority" | Should -eq "https://login.microsoftonline.com/"
                $appSettings."AzureActiveDirectoryClientSettings__TokenEndpoint" | Should -eq "/oauth2/v2.0/token"

                $appSettings."AzureActiveDirectoryClientSettings__ClientAppSettings__0__ClientId" | Should -eq $validClientSetting.clientId
                $appSettings."AzureActiveDirectoryClientSettings__ClientAppSettings__0__TenantId" | Should -eq $validClientSetting.tenantId
                $appSettings."AzureActiveDirectoryClientSettings__ClientAppSettings__0__TenantAlias" | Should -eq $validClientSetting.tenantAlias
                $appSettings."AzureActiveDirectoryClientSettings__ClientAppSettings__0__ClientSecret" | Should -eq $fabricInstallerSecret
                $appSettings."AzureActiveDirectoryClientSettings__ClientAppSettings__0__Scopes__0" | Should -eq $expectedDefaultScope
            }

            It 'Should return a hashtable of azure appsettings when multiple valid settings are added' {
                # Arrange
                $fabricInstallerSecret = "!!enc!!:some secret"

                $secondValidClientSettings = @{
                    clientId = "someclient2"
                    tenantId = "sometenant2"
                    tenantAlias = "somealias2"
                }
                $validClientSettings.Add($secondValidClientSettings)

                $validClientSettings[0].Add("clientSecret", $fabricInstallerSecret)
                $validClientSettings[1].Add("clientSecret", $fabricInstallerSecret)

                # Act
                $appSettings = Get-IdentityServiceIdPSSAzureSettings `
                    -clientSettings $validClientSettings `
                    -encryptionCert $testCert `
                    -azureSettingsConfigPath $testPath `
                    -appName $appName

                # Assert
                $appSettings.Keys.Count | Should -eq 12
                $appSettings."AzureActiveDirectoryClientSettings__Authority" | Should -eq "https://login.microsoftonline.com/"
                $appSettings."AzureActiveDirectoryClientSettings__TokenEndpoint" | Should -eq "/oauth2/v2.0/token"

                $appSettings."AzureActiveDirectoryClientSettings__ClientAppSettings__0__ClientId" | Should -eq $validClientSetting.clientId
                $appSettings."AzureActiveDirectoryClientSettings__ClientAppSettings__0__TenantId" | Should -eq $validClientSetting.tenantId
                $appSettings."AzureActiveDirectoryClientSettings__ClientAppSettings__0__TenantAlias" | Should -eq $validClientSetting.tenantAlias
                $appSettings."AzureActiveDirectoryClientSettings__ClientAppSettings__0__ClientSecret" | Should -eq $fabricInstallerSecret
                $appSettings."AzureActiveDirectoryClientSettings__ClientAppSettings__0__Scopes__0" | Should -eq $expectedDefaultScope
                
                $appSettings."AzureActiveDirectoryClientSettings__ClientAppSettings__1__ClientId" | Should -eq $secondValidClientSettings.clientId
                $appSettings."AzureActiveDirectoryClientSettings__ClientAppSettings__1__TenantId" | Should -eq $secondValidClientSettings.tenantId
                $appSettings."AzureActiveDirectoryClientSettings__ClientAppSettings__1__TenantAlias" | Should -eq $secondValidClientSettings.tenantAlias
                $appSettings."AzureActiveDirectoryClientSettings__ClientAppSettings__1__ClientSecret" | Should -eq $fabricInstallerSecret
                $appSettings."AzureActiveDirectoryClientSettings__ClientAppSettings__1__Scopes__0" | Should -eq $expectedDefaultScope
            }
        }
        Context 'Unhappy Paths' {
            BeforeAll {
                Mock -CommandName  Write-DosMessage {}
            }

            It 'Should write an error and exit early when no client settings' {
                # Arrange
                $emptyAppSettings = @{}

                # Act
                $appSettings = Get-IdentityServiceIdPSSAzureSettings

                # Assert
                Assert-MockCalled Write-DosMessage -Times 1 -Scope 'It' -ParameterFilter {
                    $Level -eq "Warning" `
                    -and $Message.StartsWith("Could not validate Azure settings, continuing without setting Azure AD. Verify Azure settings are correct in the registered applications config section: ")
                }
                $appSettings | Should -BeNullOrEmpty
            }
        }
    }
}