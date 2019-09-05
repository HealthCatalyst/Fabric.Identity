param(
    [string] $targetFilePath = "$PSScriptRoot\..\Install-Identity-Utilities.psm1"
)

Write-Host $targetFilePath
# Force re-import to pick up latest changes
Import-Module $targetFilePath -Force

Describe 'Get-IISAppPoolUser' -Tag 'Unit'{
    Context 'Quiet Mode'{
        InModuleScope Install-Identity-Utilities{
            It 'Should return stored IIS User'{
                # Arrange
                Mock -ModuleName Install-Identity-Utilities -CommandName Test-AppPoolExistsAndRunsAsUser -MockWith { $true }
                Mock -ModuleName Install-Identity-Utilities -CommandName Add-InstallationSetting -MockWith {}
                Mock -ModuleName Install-Identity-Utilities -CommandName Read-Host -MockWith {}
                Mock -ModuleName Install-Identity-Utilities -CommandName Add-InstallationSetting -MockWith { }

                # Act
                $iisUser = Get-IISAppPoolUser -credential $null -appName "identity" -storedIisUser "fabric\test.user" -installConfigPath "install.config" -scope "identity"

                # Assert
                $iisUser.UserName | Should -Be "fabric\test.user"
                Assert-MockCalled -ModuleName Install-Identity-Utilities -CommandName Read-Host -Times 0 -Exactly
            }
        }
    }

    Context 'Credential Mode'{
        InModuleScope Install-Identity-Utilities{
            It 'Should return passed in credential'{
                # Arrange
                $userName = "fabric\admin.user"
                $password = ConvertTo-SecureString "supersecretpassword" -AsPlainText -Force
                $credential = New-Object -TypeName "System.Management.Automation.PSCredential" -ArgumentList $userName, $password
                Mock -ModuleName Install-Identity-Utilities -CommandName Test-AppPoolExistsAndRunsAsUser -MockWith { $true }
                Mock -ModuleName Install-Identity-Utilities -CommandName Read-Host -MockWith { }
                Mock -ModuleName Install-Identity-Utilities -CommandName Confirm-Credentials -MockWith { }
                Mock -ModuleName Install-Identity-Utilities -CommandName Add-InstallationSetting -MockWith { }
                
                # Act
                $iisUser = Get-IISAppPoolUser -credential $credential -appName "identity" -storedIisUser "fabric\test.user" -installConfigPath "install.config" -scope "identity"

                # Assert
                $iisUser.UserName | Should -Be $userName
                $iisUser.Credential | Should -Be $credential
                Assert-MockCalled -ModuleName Install-Identity-Utilities -CommandName Read-Host -Times 0 -Exactly
                Assert-MockCalled -ModuleName Install-Identity-Utilities -CommandName Test-AppPoolExistsAndRunsAsUser -Times 0 -Exactly
                Assert-MockCalled -ModuleName Install-Identity-Utilities -CommandName Confirm-Credentials -Times 1 -Exactly
                Assert-MockCalled -ModuleName Install-Identity-Utilities -CommandName Add-InstallationSetting -Times 1 -Exactly

            }
        }
    }

    Context 'Interactive Mode'{
        InModuleScope Install-Identity-Utilities{
            It 'Should accept credentials and return a user'{
                # Arrange
                $userName = "fabric\test.user"
                $password = ConvertTo-SecureString "supersecretpassword" -AsPlainText -Force
                Mock -ModuleName Install-Identity-Utilities -CommandName Test-AppPoolExistsAndRunsAsUser -MockWith { $false }
                Mock -ModuleName Install-Identity-Utilities -CommandName Read-Host -MockWith { return $userName}
                Mock -ModuleName Install-Identity-Utilities -CommandName Read-Host -MockWith { return $password } -ParameterFilter { $AsSecureString -eq $true }
                Mock -ModuleName Install-Identity-Utilities -CommandName Get-ConfirmedCredentials -MockWith { New-Object -TypeName "System.Management.Automation.PSCredential" -ArgumentList $userName, $password }
                Mock -ModuleName Install-Identity-Utilities -CommandName Add-InstallationSetting -MockWith { }

                # Act
                $iisUser = Get-IISAppPoolUser -credential $null -appName "identity" -storedIisUser $userName -installConfigPath "install.config" -scope "identity"

                # Assert
                $iisUser.UserName | Should -Be $userName
                $iisUser.Credential.GetNetworkCredential().UserName | Should -Be "test.user"
                $iisUser.Credential.GetNetworkCredential().Password | Should -Be "supersecretpassword"
                Assert-MockCalled -ModuleName Install-Identity-Utilities -CommandName Read-Host -Times 2 -Exactly
            }

            It 'Should throw an excpetion if no user was specified'{
                # Arrange
                Mock -ModuleName Install-Identity-Utilities -CommandName Test-AppPoolExistsAndRunsAsUser -MockWith { $false }
                Mock -ModuleName Install-Identity-Utilities -CommandName Read-Host -MockWith { $null }

                # Act
                { Get-IISAppPoolUser -credential $null -appName "identity" -storedIisUser "fabric/test.user" -installConfigPath "install.config" -scope "identity"} | Should -Throw
            }
        }
    }
}
