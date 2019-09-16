param(
    [string] $targetFilePath = "$PSScriptRoot\..\Install-Identity-Utilities.psm1"
)

Write-Host $targetFilePath
# Force re-import to pick up latest changes
Import-Module $targetFilePath -Force

Describe 'Publish-Application'{
    Context 'App pool exists'{
        InModuleScope Install-Identity-Utilities{
            It 'Should install app and return version and directory'{
                # Arrange
                $userName = "Everyone"
                $password = ConvertTo-SecureString "supersecretpassword" -AsPlainText -Force
                $credential = New-Object -TypeName "System.Management.Automation.PSCredential" -ArgumentList $userName, $password
                $expectedVersion = "1.4.12345"

                $site = @{Name="Default Web Site"; physicalPath = "C:\inetpub\wwwroot"}
                $iisUser = @{UserName = $userName; Credential = $credential }

                
                Mock -ModuleName Install-Identity-Utilities -CommandName Test-AppPoolExistsAndRunsAsUser -MockWith { $true }
                Mock -ModuleName Install-Identity-Utilities -CommandName New-App -MockWith {}
                Mock -ModuleName Install-Identity-Utilities -CommandName Publish-WebSite -MockWith {}
                Mock -ModuleName Install-Identity-Utilities -CommandName New-AppPool -MockWith {}
                Mock -ModuleName Install-Identity-Utilities -CommandName Get-InstalledVersion -MockWith { $expectedVersion }

                # Act
                $publishedApp = Publish-Application -site $site -appName "identity" -iisUser $iisUser -zipPackage "$env:Temp\identity.zip" -assembly "Fabric.Identity.API.dll"

                # Assert
                $publishedApp.version = "1.4.12345"
                $publishedApp.applicationDirectory = "C:\inetpub\wwwroot\identity"
                Assert-MockCalled -ModuleName Install-Identity-Utilities -CommandName New-App -Times 1 -Exactly
                Assert-MockCalled -ModuleName Install-Identity-Utilities -CommandName Publish-WebSite -Times 1 -Exactly
                Assert-MockCalled -ModuleName Install-Identity-Utilities -CommandName New-AppPool -Times 0 -Exactly

            }
        }
    }

    Context 'App pool does not exist'{
        InModuleScope Install-Identity-Utilities{
            It 'Should install app, create app pool and return version and directory'{
                # Arrange
                $userName = "fabric\test.user"
                $password = ConvertTo-SecureString "supersecretpassword" -AsPlainText -Force
                $credential = New-Object -TypeName "System.Management.Automation.PSCredential" -ArgumentList $userName, $password
                $expectedVersion = "1.4.12345"

                $site = @{Name="Default Web Site"; physicalPath = "C:\inetpub\wwwroot"}
                $iisUser = @{UserName = $userName; Credential = $credential }

                Mock -ModuleName Install-Identity-Utilities -CommandName Test-AppPoolExistsAndRunsAsUser -MockWith { $false }
                Mock -ModuleName Install-Identity-Utilities -CommandName New-App -MockWith {}
                Mock -ModuleName Install-Identity-Utilities -CommandName Publish-WebSite -MockWith {}
                Mock -ModuleName Install-Identity-Utilities -CommandName New-AppPool -MockWith {}
                Mock -ModuleName Install-Identity-Utilities -CommandName Get-InstalledVersion -MockWith { $expectedVersion }

                # Act
                $publishedApp = Publish-Application -site $site -appName "identity" -iisUser $iisUser -zipPackage "$env:Temp\identity.zip" -assembly "Fabric.Identity.API.dll"

                # Assert
                $publishedApp.version = "1.4.12345"
                $publishedApp.applicationDirectory = "C:\inetpub\wwwroot\identity"
                Assert-MockCalled -ModuleName Install-Identity-Utilities -CommandName New-App -Times 1 -Exactly
                Assert-MockCalled -ModuleName Install-Identity-Utilities -CommandName Publish-WebSite -Times 1 -Exactly
                Assert-MockCalled -ModuleName Install-Identity-Utilities -CommandName New-AppPool -Times 1 -Exactly

            }
        }
    }
}
