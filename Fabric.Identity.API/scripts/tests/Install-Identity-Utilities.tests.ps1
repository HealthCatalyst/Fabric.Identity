param(
    [string] $targetFilePath = "$PSScriptRoot\..\Install-Identity-Utilities.psm1"
)

Write-Host $targetFilePath
# Force re-import to pick up latest changes
Import-Module $targetFilePath -Force

Describe "Identity Utilities Unit and Integration Tests" {

Describe 'Get-FullyQualifiedInstallationZipFile Unit Tests' -Tag 'Unit' {
    Context 'Zip File Exists'{
        It 'Should return working directory plus zip file when no directory is specified'{
            Mock -ModuleName Install-Identity-Utilities -CommandName Test-Path -MockWith { return $true }
            $path = Get-FullyQualifiedInstallationZipFile -zipPackage "somefile.zip" -workingDirectory $PSScriptRoot
            $path | Should -Be "$PSScriptRoot\somefile.zip"
        }

        It 'Should return full path specified when full path is specified as a parameter'{
            Mock -ModuleName Install-Identity-Utilities -CommandName Test-Path -MockWith { return $true }
            $expectedPath = "$env:TEMP\somefile.zip"
            $path = Get-FullyQualifiedInstallationZipFile -zipPackage $expectedPath -workingDirectory $PSScriptRoot
            $path | Should -Be $expectedPath
        }
    }

    Context 'Zip File Does Not Exist'{
        It 'Should throw an exception if the zip file does not exist'{
            Mock -ModuleName Install-Identity-Utilities -CommandName Test-Path -MockWith {return $false}
            {Get-FullyQualifiedInstallationZipFile -zipPackage "somefile.zip" -workingDirectory $PSScriptRoot} | Should -Throw
        }
    }
}

Describe 'Install-DotNetCoreIfNeeded' -Tag 'Unit' {
    Context 'DotNetCore version is already installed'{
        It 'Should Not Install'{
            Mock -ModuleName Install-Identity-Utilities -CommandName Test-PrerequisiteExact -MockWith { return $true }
            Mock -ModuleName Install-Identity-Utilities -CommandName Get-WebRequestDownload -MockWith {}
            Install-DotNetCoreIfNeeded -version "1.1.1234.123" -downloadUrl "http://example.com"
            Assert-MockCalled -ModuleName Install-Identity-Utilities -CommandName Get-WebRequestDownload -Times 0 -Exactly

        }
        InModuleScope Install-Identity-Utilities {
            It 'Should install proper version' {
                Mock -CommandName Test-PrerequisiteExact -MockWith { return $false }
                Mock -CommandName Get-WebRequestDownload -MockWith {}
                Mock -CommandName Start-Process -MockWith {}
                Mock -CommandName Restart-W3SVC -MockWith {}
                Mock -CommandName Remove-Item -MockWith {}
                Install-DotNetCoreIfNeeded -version "1.1.1234.123" -downloadUrl "http://example.com"
                Assert-MockCalled -CommandName Get-WebRequestDownload -Times 1 -Exactly
                Assert-MockCalled -CommandName Start-Process -Times 1 -Exactly
                Assert-MockCalled -CommandName Restart-W3SVC -Times 1 -Exactly
                Assert-MockCalled -CommandName Remove-Item -Times 1 -Exactly
            }

            It 'Should throw an exception if Start-Process throws' {
                Mock -CommandName Test-PrerequisiteExact -MockWith { return $false }
                Mock -CommandName Get-WebRequestDownload -MockWith {}
                Mock -CommandName Start-Process -MockWith { throw }
                Mock -CommandName Restart-W3SVC -MockWith {}
                Mock -CommandName Remove-Item -MockWith {}
                {Install-DotNetCoreIfNeeded -version "1.1.1234.123" -downloadUrl "http://example.com"} | Should -Throw
            }
        }
    }
}

Describe 'Get-IISWebSiteForInstall' -Tag 'Unit' {
    Context 'Quiet Mode' {
        InModuleScope Install-Identity-Utilities {
            $defaultWebSite = New-Object -TypeName psobject -Property @{Name = "Default Web Site"}
            $testWebSite =  New-Object -TypeName psobject -Property @{Name = "Test Site"}
            Mock -CommandName Get-ChildItem -MockWith { return @( $defaultWebSite, $testWebSite) }
        
            It 'Should return web site given web site name' {
                $selectedSite = Get-IISWebSiteForInstall -selectedSiteName "Default Web Site" -installConfigPath "install.config" -scope "identity" -quiet $true
                $selectedSite.Name | Should -Be "Default Web Site"
            }
            It 'Should throw an exception if given site name does not exist' {
                {Get-IISWebSiteForInstall -selectedSiteName "Bad Site" -installConfigPath "install.config" -scope "identity" -quiet $true} | Should -Throw
            }
        }
    }

    Context 'Interactive Mode - One Site' {
        InModuleScope Install-Identity-Utilities {
            It 'Should return the only web site when one web site is configured without prompting' {
                $defaultWebSite = New-Object -TypeName psobject -Property @{Name = "Default Web Site"; Id = 1}
                Mock -CommandName Get-ChildItem -MockWith { return $defaultWebSite }
                Mock -CommandName Read-Host -MockWith { }
                $selectedSite = Get-IISWebSiteForInstall -selectedSiteName "" -installConfigPath "install.config" -scope "identity" -quiet $false
                $selectedSite.Name | Should -Be "Default Web Site"
                Assert-MockCalled -CommandName Read-Host -Times 0 -Exactly
            }  
        }
    }

    Context 'Interactive Mode - Multiple Sites' {
        InModuleScope Install-Identity-Utilities {
            It 'Should return the selected web site' {
                $defaultWebSite = New-Object -TypeName psobject -Property @{Name = "Default Web Site"; Id = 1}
                $testWebSite =  New-Object -TypeName psobject -Property @{Name = "Test Site"; Id = 2}
                Mock -ModuleName Install-Identity-Utilities -CommandName Get-ChildItem -MockWith { return @( $defaultWebSite, $testWebSite) }
                Mock -ModuleName Install-Identity-Utilities -CommandName Read-Host -MockWith { 2 }
                $selectedSite = Get-IISWebSiteForInstall -selectedSiteName "" -installConfigPath "install.config" -scope "identity" -quiet $false
                $selectedSite.Name | Should -Be "Test Site"
            }  
            
            It 'Should throw an exception when an invalid site is selected' {
                $defaultWebSite = New-Object -TypeName psobject -Property @{Name = "Default Web Site"; Id = 1}
                $testWebSite =  New-Object -TypeName psobject -Property @{Name = "Test Site"; Id = 2}
                Mock -ModuleName Install-Identity-Utilities -CommandName Get-ChildItem -MockWith { return @( $defaultWebSite, $testWebSite) }
                Mock -ModuleName Install-Identity-Utilities -CommandName Read-Host -MockWith { 3 }
                Mock -ModuleName Install-Identity-Utilities -CommandName Write-DosMessage -MockWith { } -ParameterFilter { $Level -and $Level -eq "Information" -and $Message.StartsWith("You must select a web site by id between 1 and") }

                {Get-IISWebSiteForInstall -selectedSiteName "" -installConfigPath "install.config" -scope "identity" -quiet $false } | Should -Throw
                Assert-MockCalled -ModuleName Install-Identity-Utilities -CommandName Write-DosMessage -ParameterFilter { $Level -and $Level -eq "Information" -and $Message.StartsWith("You must select a web site by id between 1 and") } -Times 10 -Exactly
            }
        }
    }
}

Describe 'Get-Certificates' -Tag 'Unit'{
    Context 'Quiet Mode' {
        InModuleScope Install-Identity-Utilities {
            It 'Should return certificates without prompt'{
                Mock -ModuleName Install-Identity-Utilities -CommandName Test-ShouldShowCertMenu -MockWith { $false }
                Mock -ModuleName Install-Identity-Utilities -CommandName Get-Certificate -MockWith { return @{Thumbprint = 123456; Subject = "CN=server.domain.local"}}
                Mock -ModuleName Install-Identity-Utilities -CommandName Add-InstallationSetting -MockWith {}
                Mock -CommandName Read-Host -MockWith { }
                $certs = Get-Certificates -primarySigningCertificateThumbprint "123456" -encryptionCertificateThumbprint "123456" -installConfigPath "install.config" -scope "identity" -quiet $true
                Assert-MockCalled -ModuleName Install-Identity-Utilities -CommandName Get-Certificate -Times 2 -Exactly
                Assert-MockCalled -ModuleName Install-Identity-Utilities -CommandName Add-InstallationSetting -Times 3 -Exactly
                Assert-MockCalled -ModuleName Install-Identity-Utilities -CommandName Read-Host -Times 0 -Exactly
                $certs.SigningCertificate.Thumbprint | Should -Be "123456"
                $certs.EncryptionCertificate.Thumbprint | Should -Be "123456"
            }

            It 'Should throw an exception if we cannot read the certificate'{
                Mock -ModuleName Install-Identity-Utilities -CommandName Test-ShouldShowCertMenu -MockWith { $false }
                Mock -ModuleName Install-Identity-Utilities -CommandName Get-Certificate -MockWith { throw }
                Mock -ModuleName Install-Identity-Utilities -CommandName Add-InstallationSetting -MockWith {}
                {Get-Certificates -primarySigningCertificateThumbprint "123456" -encryptionCertificateThumbprint "123456" -installConfigPath "install.config" -scope "identity" -quiet $true } | Should -Throw
            }
        }
    }

    Context 'Interactive Mode'{
        InModuleScope Install-Identity-Utilities{
            It 'Should prompt and return certificates'{
                # Arrange
                $testBeforeDate = Get-Date
                $testAfterDate = Get-Date
                $cert1 = New-Object -TypeName psobject -Property @{Thumbprint = 678901; Subject = "CN=server.domain.local"; NotBefore = $testBeforeDate.AddDays(-10); NotAfter = $testAfterDate.AddDays(10)}
                $cert2 =  New-Object -TypeName psobject -Property @{Thumbprint = 123456; Subject = "CN=server.domain.local"; NotBefore = $testBeforeDate.AddDays(-10); NotAfter = $testAfterDate.AddDays(10)}
                Mock -ModuleName Install-Identity-Utilities -CommandName Get-CertsFromLocation -MockWith { return @($cert1, $cert2)}
                Mock -ModuleName Install-Identity-Utilities -CommandName Test-ShouldShowCertMenu -MockWith { $true }
                Mock -ModuleName Install-Identity-Utilities -CommandName Get-Certificate -MockWith { return @{Thumbprint = 123456; Subject = "CN=server.domain.local"}}
                Mock -ModuleName Install-Identity-Utilities -CommandName Add-InstallationSetting -MockWith {}
                Mock -CommandName Read-Host -MockWith { 2 }

                # Act
                $certs = Get-Certificates -primarySigningCertificateThumbprint "123456" -encryptionCertificateThumbprint "123456" -installConfigPath "install.config" -scope "identity" -quiet $true

                # Assert
                Assert-MockCalled -ModuleName Install-Identity-Utilities -CommandName Get-Certificate -Times 2 -Exactly
                Assert-MockCalled -ModuleName Install-Identity-Utilities -CommandName Add-InstallationSetting -Times 3 -Exactly
                Assert-MockCalled -ModuleName Install-Identity-Utilities -CommandName Read-Host -Times 1 -Exactly
                $certs.SigningCertificate.Thumbprint | Should -Be "123456"
                $certs.EncryptionCertificate.Thumbprint | Should -Be "123456"
            }

            It 'Should throw an exception if no selection is made'{
                # Arrange
                $cert1 = New-Object -TypeName psobject -Property @{Thumbprint = 678901; Subject = "CN=server.domain.local"}
                $cert2 =  New-Object -TypeName psobject -Property @{Thumbprint = 123456; Subject = "CN=server.domain.local"}
                Mock -ModuleName Install-Identity-Utilities -CommandName Get-CertsFromLocation -MockWith { return @($cert1, $cert2)}
                Mock -ModuleName Install-Identity-Utilities -CommandName Test-ShouldShowCertMenu -MockWith { $true }
                Mock -ModuleName Install-Identity-Utilities -CommandName Get-Certificate -MockWith { return @{Thumbprint = 123456; Subject = "CN=server.domain.local"}}
                Mock -ModuleName Install-Identity-Utilities -CommandName Add-InstallationSetting -MockWith {}
                Mock -ModuleName Install-Identity-Utilities -CommandName Write-DosMessage -MockWith { } -ParameterFilter { $Level -and $Level -eq "Information" -and $Message -eq "You must select a certificate so Fabric.Identity can sign access and identity tokens." }
                Mock -CommandName Read-Host -MockWith { $null }

                # Act/Assert
                {Get-Certificates -primarySigningCertificateThumbprint "123456" -encryptionCertificateThumbprint "123456" -installConfigPath "install.config" -scope "identity" -quiet $false} | Should -Throw
                Assert-MockCalled -ModuleName Install-Identity-Utilities -CommandName Write-DosMessage -ParameterFilter { $Level -and $Level -eq "Information" -and $Message -eq "You must select a certificate so Fabric.Identity can sign access and identity tokens." } -Times 10 -Exactly
            }

            It 'Should throw an exception if a bad selection is made'{
                # Arrange
                $cert1 = New-Object -TypeName psobject -Property @{Thumbprint = 678901; Subject = "CN=server.domain.local"}
                $cert2 =  New-Object -TypeName psobject -Property @{Thumbprint = 123456; Subject = "CN=server.domain.local"}
                Mock -ModuleName Install-Identity-Utilities -CommandName Get-CertsFromLocation -MockWith { return @($cert1, $cert2)}
                Mock -ModuleName Install-Identity-Utilities -CommandName Test-ShouldShowCertMenu -MockWith { $true }
                Mock -ModuleName Install-Identity-Utilities -CommandName Get-Certificate -MockWith { return @{Thumbprint = 123456; Subject = "CN=server.domain.local"}}
                Mock -ModuleName Install-Identity-Utilities -CommandName Add-InstallationSetting -MockWith {}
                Mock -ModuleName Install-Identity-Utilities -CommandName Write-DosMessage -MockWith { } -ParameterFilter { $Level -and $Level -eq "Information" -and $Message.StartsWith("Please select a certificate with index between 1 and") }
                Mock -CommandName Read-Host -MockWith { 3 }

                # Act/Assert
                {Get-Certificates -primarySigningCertificateThumbprint "123456" -encryptionCertificateThumbprint "123456" -installConfigPath "install.config" -scope "identity" -quiet $false} | Should -Throw
                Assert-MockCalled -ModuleName Install-Identity-Utilities -CommandName Write-DosMessage -ParameterFilter { $Level -and $Level -eq "Information" -and $Message.StartsWith("Please select a certificate with index between 1 and") } -Times 10 -Exactly
            }

            It 'Should throw an exception if certs cannot be retreived from certificate store'{
                # Arrange
                Mock -ModuleName Install-Identity-Utilities -CommandName Get-CertsFromLocation -MockWith { throw }
                Mock -ModuleName Install-Identity-Utilities -CommandName Test-ShouldShowCertMenu -MockWith { $true }

                # Act/Assert
                {Get-Certificates -primarySigningCertificateThumbprint "123456" -encryptionCertificateThumbprint "123456" -installConfigPath "install.config" -scope "identity" -quiet $false} | Should -Throw
            }
        }
    }
}

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

Describe 'Get-AppInsightsKey'{
    Context 'Quiet Mode'{
        InModuleScope Install-Identity-Utilities{
            It 'Should return stored app insights key'{
                # Arrange
                Mock -ModuleName Install-Identity-Utilities -CommandName Read-Host -MockWith { }
                Mock -ModuleName Install-Identity-Utilities -CommandName Add-InstallationSetting -MockWith {}
                
                # Act
                $appInsightsKey = Get-AppInsightsKey -appInsightsInstrumentationKey "123456" -installConfigPath "install.config" -scope "identity" -quiet $true

                # Assert
                $appInsightsKey | Should -Be "123456"
                Assert-MockCalled -ModuleName Install-Identity-Utilities -CommandName Read-Host -Times 0 -Exactly
                Assert-MockCalled -ModuleName Install-Identity-Utilities -CommandName Add-InstallationSetting -Times 2 -Exactly
            }
        }
    }

    Context 'Interactive Mode'{
        InModuleScope Install-Identity-Utilities{
            It 'Should return user entered app insights key'{
                # Arrange
                Mock -ModuleName Install-Identity-Utilities -CommandName Read-Host -MockWith { return "567890" }
                Mock -ModuleName Install-Identity-Utilities -CommandName Add-InstallationSetting -MockWith {}

                # Act
                $appInsightsKey = Get-AppInsightsKey -appInsightsInstrumentationKey "123456" -installConfigPath "install.config" -scope "identity" -quiet $false

                # Assert
                $appInsightsKey | Should -Be "567890"
                Assert-MockCalled -ModuleName Install-Identity-Utilities -CommandName Read-Host -Times 1 -Exactly
                Assert-MockCalled -ModuleName Install-Identity-Utilities -CommandName Add-InstallationSetting -Times 2 -Exactly
            }
        }
    }
}

Describe 'Get-SqlServerAddress'{
    Context 'Quiet Mode'{
        InModuleScope Install-Identity-Utilities{
            It 'Should return stored sql server address'{
                # Arrange
                Mock -ModuleName Install-Identity-Utilities -CommandName Read-Host -MockWith { }
                Mock -ModuleName Install-Identity-Utilities -CommandName Add-InstallationSetting -MockWith {}
                
                # Act
                $sqlServerAddress = Get-SqlServerAddress -sqlServerAddress "somemachine.fabric.local" -installConfigPath "install.config" -quiet $true

                # Assert
                $sqlServerAddress | Should -Be "somemachine.fabric.local"
                Assert-MockCalled -ModuleName Install-Identity-Utilities -CommandName Read-Host -Times 0 -Exactly
                Assert-MockCalled -ModuleName Install-Identity-Utilities -CommandName Add-InstallationSetting -Times 1 -Exactly
            }
        }
    }

    Context 'Interactive Mode'{
        InModuleScope Install-Identity-Utilities{
            It 'Should return user entered sql server address'{
                # Arrange
                Mock -ModuleName Install-Identity-Utilities -CommandName Read-Host -MockWith { return "othermachine.fabric.local" }
                Mock -ModuleName Install-Identity-Utilities -CommandName Add-InstallationSetting -MockWith {}

                # Act
                $sqlServerAddress = Get-SqlServerAddress -sqlServerAddress "somemachine.fabric.local" -installConfigPath "install.config" -quiet $false

                # Assert
                $sqlServerAddress | Should -Be "othermachine.fabric.local"
                Assert-MockCalled -ModuleName Install-Identity-Utilities -CommandName Read-Host -Times 1 -Exactly
                Assert-MockCalled -ModuleName Install-Identity-Utilities -CommandName Add-InstallationSetting -Times 1 -Exactly
            }
        }
    }
}

Describe 'Get-DiscoveryServiceUrl'{
    Context 'Quiet Mode'{
        InModuleScope Install-Identity-Utilities{
            It 'Should return stored DiscoveryService URL'{
                # Arrange
                Mock -ModuleName Install-Identity-Utilities -CommandName Get-FullyQualifiedMachineName -MockWith { }
                Mock -ModuleName Install-Identity-Utilities -CommandName Read-Host -MockWith { }
                Mock -ModuleName Install-Identity-Utilities -CommandName Add-InstallationSetting -MockWith {}
                
                # Act
                $discoUrl = Get-DiscoveryServiceUrl -discoveryServiceUrl "https://host.fabric.local/DiscoveryService/v1" -installConfigPath "install.config" -quiet $true

                # Assert
                $discoUrl | Should -Be "https://host.fabric.local/DiscoveryService/v1"
                Assert-MockCalled -ModuleName Install-Identity-Utilities -CommandName Read-Host -Times 0 -Exactly
                Assert-MockCalled -ModuleName Install-Identity-Utilities -CommandName Get-FullyQualifiedMachineName -Times 0 -Exactly
                Assert-MockCalled -ModuleName Install-Identity-Utilities -CommandName Add-InstallationSetting -Times 1 -Exactly
            }
        }
    }

    Context 'Interactive Mode'{
        InModuleScope Install-Identity-Utilities{
            It 'Should return user DiscoveryService URL'{
                # Arrange
                Mock -ModuleName Install-Identity-Utilities -CommandName Get-FullyQualifiedMachineName -MockWith { }
                Mock -ModuleName Install-Identity-Utilities -CommandName Read-Host -MockWith { "https://otherhost.fabric.local/DiscoveryService/v1" }
                Mock -ModuleName Install-Identity-Utilities -CommandName Add-InstallationSetting -MockWith {}
                
                # Act
                $discoUrl = Get-DiscoveryServiceUrl -discoveryServiceUrl "https://host.fabric.local/DiscoveryService/v1" -installConfigPath "install.config" -quiet $false

                # Assert
                $discoUrl | Should -Be "https://otherhost.fabric.local/DiscoveryService/v1"
                Assert-MockCalled -ModuleName Install-Identity-Utilities -CommandName Read-Host -Times 1 -Exactly
                Assert-MockCalled -ModuleName Install-Identity-Utilities -CommandName Get-FullyQualifiedMachineName -Times 0 -Exactly
                Assert-MockCalled -ModuleName Install-Identity-Utilities -CommandName Add-InstallationSetting -Times 1 -Exactly
            }
        }
    }
}

Describe 'Get-ApplicationEndpoint'{
    Context 'Quiet Mode'{
        InModuleScope Install-Identity-Utilities{
            It 'Should return stored ApplicationEndpoint'{
                # Arrange
                Mock -ModuleName Install-Identity-Utilities -CommandName Get-FullyQualifiedMachineName -MockWith { }
                Mock -ModuleName Install-Identity-Utilities -CommandName Read-Host -MockWith { }
                Mock -ModuleName Install-Identity-Utilities -CommandName Add-InstallationSetting -MockWith {}
                
                # Act
                $appEndpoint = Get-ApplicationEndpoint -appName "identity" -applicationEndpoint "https://host.fabric.local/identity" -installConfigPath "install.config" -scope "identity" -quiet $true

                # Assert
                $appEndpoint | Should -Be "https://host.fabric.local/identity"
                Assert-MockCalled -ModuleName Install-Identity-Utilities -CommandName Read-Host -Times 0 -Exactly
                Assert-MockCalled -ModuleName Install-Identity-Utilities -CommandName Get-FullyQualifiedMachineName -Times 0 -Exactly
                Assert-MockCalled -ModuleName Install-Identity-Utilities -CommandName Add-InstallationSetting -Times 2 -Exactly
            }
        }
    }

    Context 'Interactive Mode'{
        InModuleScope Install-Identity-Utilities{
            It 'Should return user DiscoveryService URL'{
                # Arrange
                Mock -ModuleName Install-Identity-Utilities -CommandName Get-FullyQualifiedMachineName -MockWith { }
                Mock -ModuleName Install-Identity-Utilities -CommandName Read-Host -MockWith { "https://otherhost.fabric.local/identity" }
                Mock -ModuleName Install-Identity-Utilities -CommandName Add-InstallationSetting -MockWith {}
                
                # Act
                $appEndpoint = Get-ApplicationEndpoint -appName "identity" -applicationEndpoint "https://host.fabric.local/identity" -installConfigPath "install.config" -scope "identity" -quiet $false

                # Assert
                $appEndpoint | Should -Be "https://otherhost.fabric.local/identity"
                Assert-MockCalled -ModuleName Install-Identity-Utilities -CommandName Read-Host -Times 1 -Exactly
                Assert-MockCalled -ModuleName Install-Identity-Utilities -CommandName Get-FullyQualifiedMachineName -Times 0 -Exactly
                Assert-MockCalled -ModuleName Install-Identity-Utilities -CommandName Add-InstallationSetting -Times 2 -Exactly
            }
        }
    }
}

Describe 'Get-IdentityDatabaseConnectionString'{
    Context 'Quiet Mode'{
        InModuleScope Install-Identity-Utilities{
            It 'Should return stored Identity db connection string'{
                # Arrange
                Mock -ModuleName Install-Identity-Utilities -CommandName Invoke-Sql -MockWith {}
                Mock -ModuleName Install-Identity-Utilities -CommandName Add-InstallationSetting -MockWith {}
                Mock -ModuleName Install-Identity-Utilities -CommandName Read-Host -MockWith {}

                # Act
                $dbconnectionString = Get-IdentityDatabaseConnectionString -identityDbName "identity" -sqlServerAddress "host.fabric.local" -installConfigPath "install.config" -quiet $true

                # Assert
                $dbConnectionString.DbName | Should -Be "identity"
                $dbConnectionString.DbConnectionString | Should -Be "Server=host.fabric.local;Database=identity;Trusted_Connection=True;MultipleActiveResultSets=True;"
                Assert-MockCalled -ModuleName Install-Identity-Utilities -CommandName Read-Host -Times 0 -Exactly
                Assert-MockCalled -ModuleName Install-Identity-Utilities -CommandName Invoke-Sql -Times 1 -Exactly
                Assert-MockCalled -ModuleName Install-Identity-Utilities -CommandName Add-InstallationSetting -Times 1 -Exactly
            }

            It 'Should throw when cannot connect to database'{
                # Arrange
                Mock -ModuleName Install-Identity-Utilities -CommandName Invoke-Sql -MockWith { throw }
                Mock -ModuleName Install-Identity-Utilities -CommandName Add-InstallationSetting -MockWith {}
                Mock -ModuleName Install-Identity-Utilities -CommandName Read-Host -MockWith {}

                # Act/Assert
                {Get-IdentityDatabaseConnectionString -identityDbName "identity" -sqlServerAddress "host.fabric.local" -installConfigPath "install.config" -quiet $true } | Should -Throw
            }
        }
    }

    Context 'Interactive Mode'{
        InModuleScope Install-Identity-Utilities{
            It 'Should return user entered Identity db connection string'{
                # Arrange
                Mock -ModuleName Install-Identity-Utilities -CommandName Invoke-Sql -MockWith {}
                Mock -ModuleName Install-Identity-Utilities -CommandName Add-InstallationSetting -MockWith {}
                Mock -ModuleName Install-Identity-Utilities -CommandName Read-Host -MockWith { "identity2" }

                # Act
                $dbconnectionString = Get-IdentityDatabaseConnectionString -identityDbName "identity" -sqlServerAddress "host.fabric.local" -installConfigPath "install.config" -quiet $false

                # Assert
                $dbConnectionString.DbName | Should -Be "identity2"
                $dbConnectionString.DbConnectionString | Should -Be "Server=host.fabric.local;Database=identity2;Trusted_Connection=True;MultipleActiveResultSets=True;"
                Assert-MockCalled -ModuleName Install-Identity-Utilities -CommandName Read-Host -Times 1 -Exactly
                Assert-MockCalled -ModuleName Install-Identity-Utilities -CommandName Invoke-Sql -Times 1 -Exactly
                Assert-MockCalled -ModuleName Install-Identity-Utilities -CommandName Add-InstallationSetting -Times 1 -Exactly
            }
        }
    }
}

Describe 'Get-MetadataDatabaseConnectionString'{
    Context 'Quiet Mode'{
        InModuleScope Install-Identity-Utilities{
            It 'Should return stored Metadata db connection string'{
                # Arrange
                Mock -ModuleName Install-Identity-Utilities -CommandName Invoke-Sql -MockWith {}
                Mock -ModuleName Install-Identity-Utilities -CommandName Add-InstallationSetting -MockWith {}
                Mock -ModuleName Install-Identity-Utilities -CommandName Read-Host -MockWith {}

                # Act
                $dbconnectionString = Get-MetadataDatabaseConnectionString -metadataDbName "EDWAdmin" -sqlServerAddress "host.fabric.local" -installConfigPath "install.config" -quiet $true

                # Assert
                $dbConnectionString.DbName | Should -Be "EDWAdmin"
                $dbConnectionString.DbConnectionString | Should -Be "Server=host.fabric.local;Database=EDWAdmin;Trusted_Connection=True;MultipleActiveResultSets=True;"
                Assert-MockCalled -ModuleName Install-Identity-Utilities -CommandName Read-Host -Times 0 -Exactly
                Assert-MockCalled -ModuleName Install-Identity-Utilities -CommandName Invoke-Sql -Times 1 -Exactly
                Assert-MockCalled -ModuleName Install-Identity-Utilities -CommandName Add-InstallationSetting -Times 1 -Exactly
            }

            It 'Should throw when cannot connect to database'{
                # Arrange
                Mock -ModuleName Install-Identity-Utilities -CommandName Invoke-Sql -MockWith { throw }
                Mock -ModuleName Install-Identity-Utilities -CommandName Add-InstallationSetting -MockWith {}
                Mock -ModuleName Install-Identity-Utilities -CommandName Read-Host -MockWith {}

                # Act/Assert
                {Get-MetadataDatabaseConnectionString -metadataDbName "EDWAdmin" -sqlServerAddress "host.fabric.local" -installConfigPath "install.config" -quiet $true } | Should -Throw
            }
        }
    }

    Context 'Interactive Mode'{
        InModuleScope Install-Identity-Utilities{
            It 'Should return user entered Metadata db connection string'{
                # Arrange
                Mock -ModuleName Install-Identity-Utilities -CommandName Invoke-Sql -MockWith {}
                Mock -ModuleName Install-Identity-Utilities -CommandName Add-InstallationSetting -MockWith {}
                Mock -ModuleName Install-Identity-Utilities -CommandName Read-Host -MockWith { "EDWAdmin2" }

                # Act
                $dbconnectionString = Get-MetadataDatabaseConnectionString -metadataDbName "EDWAdmin" -sqlServerAddress "host.fabric.local" -installConfigPath "install.config" -quiet $false

                # Assert
                $dbConnectionString.DbName | Should -Be "EDWAdmin2"
                $dbConnectionString.DbConnectionString | Should -Be "Server=host.fabric.local;Database=EDWAdmin2;Trusted_Connection=True;MultipleActiveResultSets=True;"
                Assert-MockCalled -ModuleName Install-Identity-Utilities -CommandName Read-Host -Times 1 -Exactly
                Assert-MockCalled -ModuleName Install-Identity-Utilities -CommandName Invoke-Sql -Times 1 -Exactly
                Assert-MockCalled -ModuleName Install-Identity-Utilities -CommandName Add-InstallationSetting -Times 1 -Exactly
            }
        }
    }
}

Describe 'Publish-Application'{
    Context 'App pool exists'{
        InModuleScope Install-Identity-Utilities{
            It 'Should install app and return version and directory'{
                # Arrange
                $userName = "fabric\test.user"
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

Describe 'Get-DefaultDiscoveryServiceUrl'{
    Context 'Returns stored URL'{
        InModuleScope Install-Identity-Utilities{
            It 'Should return the stored URL as is'{
                # Arrange
                Mock -ModuleName Install-Identity-Utilities -CommandName Get-FullyQualifiedMachineName -MockWith {}

                # Act
                $discoUrl = Get-DefaultDiscoveryServiceUrl -discoUrl "https://host.fabric.local/DiscoveryService/v1"

                # Assert
                $discoUrl | Should -Be "https://host.fabric.local/DiscoveryService/v1"
                Assert-MockCalled -ModuleName Install-Identity-Utilities -CommandName Get-FullyQualifiedMachineName -Times 0 -Exactly
            }
            It 'Should return the stored URL after trimming the trailing slash'{
                # Arrange
                Mock -ModuleName Install-Identity-Utilities -CommandName Get-FullyQualifiedMachineName -MockWith {}

                # Act
                $discoUrl = Get-DefaultDiscoveryServiceUrl -discoUrl "https://host.fabric.local/DiscoveryService/v1/"

                # Assert
                $discoUrl | Should -Be "https://host.fabric.local/DiscoveryService/v1"
                Assert-MockCalled -ModuleName Install-Identity-Utilities -CommandName Get-FullyQualifiedMachineName -Times 0 -Exactly
            }
            It 'Should return the stored URL after appending a v1'{
                # Arrange
                Mock -ModuleName Install-Identity-Utilities -CommandName Get-FullyQualifiedMachineName -MockWith {}

                # Act
                $discoUrl = Get-DefaultDiscoveryServiceUrl -discoUrl "https://host.fabric.local/DiscoveryService/"

                # Assert
                $discoUrl | Should -Be "https://host.fabric.local/DiscoveryService/v1"
                Assert-MockCalled -ModuleName Install-Identity-Utilities -CommandName Get-FullyQualifiedMachineName -Times 0 -Exactly
            }
        }
    }
    Context 'Returns calculated URL'{
        InModuleScope Install-Identity-Utilities{
            It 'Should return the calculated discovery URL'{
                # Arrange
                Mock -ModuleName Install-Identity-Utilities -CommandName Get-FullyQualifiedMachineName -MockWith { "https://host.fabric.local"}

                # Act
                $discoUrl = Get-DefaultDiscoveryServiceUrl -discoUrl $null

                # Assert
                $discoUrl | Should -Be "https://host.fabric.local/DiscoveryService/v1"
                Assert-MockCalled -ModuleName Install-Identity-Utilities -CommandName Get-FullyQualifiedMachineName -Times 1 -Exactly
            }
        }
    }
}

Describe 'Get-DefaultApplicationEndpoint'{
    Context 'Gets stored app endpoint'{
        InModuleScope Install-Identity-Utilities{
            It 'Should return the stored app endpoint'{
                # Arrange
                Mock -ModuleName Install-Identity-Utilities -CommandName Get-FullyQualifiedMachineName -MockWith {}

                # Act
                $appEndpoint = Get-DefaultApplicationEndpoint -appName "identity" -appEndpoint "https://host.fabric.local/identity"

                # Assert
                $appEndpoint | Should -Be "https://host.fabric.local/identity"
                Assert-MockCalled -ModuleName Install-Identity-Utilities -CommandName Get-FullyQualifiedMachineName -Times 0 -Exactly
            }
        }
    }
    Context 'Gets calculated app endpoint'{
        InModuleScope Install-Identity-Utilities{
            It 'Should return the calculated app endpoint'{
                # Arrange
                Mock -ModuleName Install-Identity-Utilities -CommandName Get-FullyQualifiedMachineName -MockWith { "https://host.fabric.local" }

                # Act
                $appEndpoint = Get-DefaultApplicationEndpoint -appName "identity" -appEndpoint $null

                # Assert
                $appEndpoint | Should -Be "https://host.fabric.local/identity"
                Assert-MockCalled -ModuleName Install-Identity-Utilities -CommandName Get-FullyQualifiedMachineName -Times 1 -Exactly
            }
        }
    }
}

Describe 'Test-ShouldShowCertMenu'{
    InModuleScope Install-Identity-Utilities{
        Context 'Interactive Mode'{
            It 'Should return true when in interactive mode and signing cert thumbprint is not present'{
                $shouldShow = Test-ShouldShowCertMenu -primarySigningCertificateThumbprint "" -encryptionCertificateThumbprint "12345" -quiet $false
                $shouldShow | Should -Be $true
            }
            It 'Should return true when in interactive mode and encryption cert thumbprint is not present'{
                $shouldShow = Test-ShouldShowCertMenu -primarySigningCertificateThumbprint "12345" -encryptionCertificateThumbprint "" -quiet $false
                $shouldShow | Should -Be $true
            }
            It 'Should return false when in interactive mode and encryption and signing cert thumbprint are present'{
                $shouldShow = Test-ShouldShowCertMenu -primarySigningCertificateThumbprint "12345" -encryptionCertificateThumbprint "12345" -quiet $false
                $shouldShow | Should -Be $false
            }
            It 'Should return true when in interactive mode and no thumbprints are present'{
                $shouldShow = Test-ShouldShowCertMenu -primarySigningCertificateThumbprint "" -encryptionCertificateThumbprint "" -quiet $false
                $shouldShow | Should -Be $true
            }
        }
        Context 'Quiet Mode'{
            It 'Should return false when in quiet mode and signing and encryption cert thumbprnts are present'{
                $shouldShow = Test-ShouldShowCertMenu -primarySigningCertificateThumbprint "12345" -encryptionCertificateThumbprint "12345" -quiet $true
                $shouldShow | Should -Be $false
            }
            It 'Should return false when in quiet mode and signing cert thumbprint is not present'{
                $shouldShow = Test-ShouldShowCertMenu -primarySigningCertificateThumbprint "" -encryptionCertificateThumbprint "12345" -quiet $true
                $shouldShow | Should -Be $false
            }
            It 'Should return false when in quiet mode and encryption cert thumbprint is not present'{
                $shouldShow = Test-ShouldShowCertMenu -primarySigningCertificateThumbprint "12345" -encryptionCertificateThumbprint "" -quiet $true
                $shouldShow | Should -Be $false
            }
            It 'Should return false when in quiet mode and no thumbprints are present'{
                $shouldShow = Test-ShouldShowCertMenu -primarySigningCertificateThumbprint "" -encryptionCertificateThumbprint "" -quiet $true
                $shouldShow | Should -Be $false
            }
        }
    }
}

Describe 'Get-ClientSettingsFromInstallConfig' -Tag 'Unit' {
    InModuleScope Install-Identity-Utilities{
    Context 'Valid config path' {
        It 'Should return a list of client settings' {
            $mockXml = [xml]'<?xml version="1.0" encoding="utf-8"?><installation><settings><scope name="identity"><variable name="fabricInstallerSecret" value="" /><variable name="discoveryService" value="" />	<registeredApplications><variable appName="testApp" tenantId="tenant1" secret="secret1" clientid="clientid1" /><variable appName="testApp" tenantId="tenant2" secret="secret2" clientid="clientid2" /></registeredApplications></scope></settings></installation>'

            Mock -CommandName Get-Content { return $mockXml }
            $result = Get-ClientSettingsFromInstallConfig -installConfigPath "install.config" -appName "testApp"
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
}

Describe 'Test-DiscoveryService Unit Tests'{
    InModuleScope Install-Identity-Utilities{
    Context 'Success'{
        It 'Should succeed and not throw an exception'{
            # Arrange
            Mock Invoke-RestMethod {}

            # Act/Assert
            {Test-DiscoveryService -discoveryBaseUrl "https://host.domain.local/DiscoveryService" } | Should -Not -Throw
            Assert-MockCalled Invoke-RestMethod -Times 1 -ParameterFilter { $Uri -eq "https://host.domain.local/DiscoveryService/v1/Services" }
        }
      }
    Context 'Generic Exception'{
        It 'Should fail and throw an exception'{
            # Arrange
            Mock Invoke-RestMethod { throw "bad stuff happened" } 

            # Act/Assert
            {Test-DiscoveryService -discoveryBaseUrl "https://host.domain.local/DiscoveryService" } | Should -Throw
        }
    }
  }
}

Describe 'Confirm-DiscoveryConfig Unit Tests'{
    InModuleScope Install-Identity-Utilities{
        It 'Should validate valid discovery config settings'{
            # Arrange
            $discoveryConfig = @{ appName = "DiscoveryService"; appPoolName = "DiscoveryService"; siteName = "Default Web Site"}
            $commonConfig = @{ sqlServerAddress = "localhost"; metadataDbName = "EDWAdmin"; webServerDomain = "host.domain.local"; clientEnvironment = "dev" }

            # Act/Assert
            { Confirm-DiscoveryConfig -discoveryConfig $discoveryConfig -commonConfig $commonConfig } | Should -Not -Throw
        }
        It 'Should throw if there is an invalid setting'{
            # Arrange
            $discoveryConfig = @{}
            $commonConfig = @{}

            # Act/Assert
            { Confirm-DiscoveryConfig -discoveryConfig $discoveryConfig -commonConfig $commonConfig } | Should -Throw
        }
    }
}

# Need to use global variables in Pester when abstracting BeforeEach and AfterEach Setup Code
# $TestDrive is not accessible in a Global variable, only in the Describe BeforeEach and AfterEach
$Global:testInstallFile = "install.config"
$Global:testAzureFile = "testAzure.config"
$Global:testInstallFileLoc = '.\tests\install.config'
$Global:testAzureFileLoc = '.\tests\testAzure.config'
$Global:installConfigPath
$Global:azureConfigPath
$Global:nodesToSearch = @("tenants","replyUrls","claimsIssuerTenant","allowedTenants","registeredApplications", "azureSecretName")
Describe 'Migrate-AADSettings' -Tag 'Unit'{
    BeforeEach{
        # Arrange 
        # Add to the powershell TestDrive which cleans up after each context, leaving the tests folder configs unchanged
        $Global:installConfigPath = "$($TestDrive)\$($testInstallFile)"
        $Global:azureConfigPath = "$($TestDrive)\$($testAzureFile)"
        $doesInstallFileExist = Test-Path $installConfigPath
        $doesAzureFileExist = Test-Path $azureConfigPath
        if (!$doesInstallFileExist)
        {
        Get-Content $testInstallFileLoc | Out-File $installConfigPath
        }
        if (!$doesAzureFileExist)
        {
        Get-Content $testAzureFileLoc | Out-File $azureConfigPath
        }
    }
    AfterEach{
        # test file will exist within the same context, so it needs to be blown away
        $doesInstallFileExist = Test-Path $installConfigPath
        $doesAzureFileExist = Test-Path $azureConfigPath
        if ($doesInstallFileExist)
        {
            Remove-Item $installConfigPath
        }
        if ($doesAzureFileExist)
        {
            Remove-Item $azureConfigPath
        }
    } 
    Context 'Migrating AAD Settings using Unit Tests'{
        InModuleScope Install-Identity-Utilities{
            It 'Should successfully run the migration'{
                # Arrange
                Mock -ModuleName Install-Identity-Utilities -CommandName Search-XMLChildNode -MockWith {$true}

                $xmlChildNodes = Get-XMLChildNodes -installConfigPath $installConfigPath -configSection "identity" -nodesToSearch $nodesToSearch -childNodeGetAttribute "name"

                Mock -ModuleName Install-Identity-Utilities -CommandName Remove-XMLChildNodes -MockWith {}
                Mock -ModuleName Install-Identity-Utilities -CommandName Add-XMLChildNodes -MockWith {}
                Mock -ModuleName Install-Identity-Utilities -CommandName Compare-Object -MockWith {}

                # Act
                $runResult = Migrate-AADSettings -installConfigPath $installConfigPath -azureConfigPath $azureConfigPath -nodesToSearch $nodesToSearch
                
                # Assert
                $runResult | Should -Be $true
                $xmlChildNodes.Count | Should -Be 6
                Assert-MockCalled -ModuleName Install-Identity-Utilities -CommandName Search-XMLChildNode -Scope It -Times 1 -Exactly
                Assert-MockCalled -ModuleName Install-Identity-Utilities -CommandName Remove-XMLChildNodes -Scope It -Times 3 -Exactly
                Assert-MockCalled -ModuleName Install-Identity-Utilities -CommandName Add-XMLChildNodes -Scope It -Times 1 -Exactly
                Assert-MockCalled -ModuleName Install-Identity-Utilities -CommandName Compare-Object -Scope It -Times 1 -Exactly
            }
            It 'Should not run the migration if tenant child node doesnt exist'{
                # Arrange
                Mock -ModuleName Install-Identity-Utilities -CommandName Search-XMLChildNode -MockWith {$false}

                # Act
                $runResult = Migrate-AADSettings -installConfigPath $installConfigPath -azureConfigPath $azureConfigPath -nodesToSearch $nodesToSearch
                
                # Assert
                $runResult | Should -Be $false
                Assert-MockCalled -ModuleName Install-Identity-Utilities -CommandName Search-XMLChildNode -Scope It -Times 1 -Exactly
            }
            It 'Should succeed but not remove install.config settings if comparison files are different'{
                # Arrange
                $customObject = [PSCustomObject]@{
                 Path = ".\"
                 Owner = "TestOwner"
                 Group = "TestUsers"
                 AccessType = "Allow"
                 Rights = "ModifyData"
                 ObjectDifference = "<variable name = 'newitem' value = 'newvalue'/>"
                }

                Mock -ModuleName Install-Identity-Utilities -CommandName Search-XMLChildNode -MockWith {$true}

                $xmlChildNodes = Get-XMLChildNodes -installConfigPath "install.config" -configSection "identity" -nodesToSearch $nodesToSearch -childNodeGetAttribute "name"

                Mock -ModuleName Install-Identity-Utilities -CommandName Remove-XMLChildNodes -MockWith {}
                Mock -ModuleName Install-Identity-Utilities -CommandName Add-XMLChildNodes -MockWith {}
                Mock -ModuleName Install-Identity-Utilities -CommandName Compare-Object -MockWith {$customObject}

                # Act
                $runResult = Migrate-AADSettings -installConfigPath $installConfigPath -azureConfigPath $azureConfigPath -nodesToSearch $nodesToSearch
                
                # Assert
                $runResult | Should -Be $true
                $xmlChildNodes.Count | Should -Be 6
                Assert-MockCalled -ModuleName Install-Identity-Utilities -CommandName Search-XMLChildNode -Scope It -Times 1 -Exactly
                Assert-MockCalled -ModuleName Install-Identity-Utilities -CommandName Remove-XMLChildNodes -Scope It -Times 2 -Exactly
                Assert-MockCalled -ModuleName Install-Identity-Utilities -CommandName Add-XMLChildNodes -Scope It -Times 1 -Exactly
                Assert-MockCalled -ModuleName Install-Identity-Utilities -CommandName Compare-Object -Scope It -Times 1 -Exactly
            }
        }
    }
}
Describe 'Migrate-AADSettings' -Tag 'Integration'{
    BeforeEach{
        # Arrange 
        # Add to the powershell TestDrive which cleans up after each context, leaving the tests folder configs unchanged
        
        $Global:testInstallFile = "testInstall.config"
        $Global:testInstallFileLoc = '.\tests\testInstall.config'
        $Global:installConfigPath = "$($TestDrive)\$($testInstallFile)"
        $Global:azureConfigPath = "$($TestDrive)\$($testAzureFile)"
        $doesInstallFileExist = Test-Path $installConfigPath
        $doesAzureFileExist = Test-Path $azureConfigPath
        if (!$doesInstallFileExist)
        {
        Get-Content "$testInstallFileLoc" | Out-File $installConfigPath
        }
        if (!$doesAzureFileExist)
        {
        Get-Content "$testAzureFileLoc" | Out-File $azureConfigPath
        }
    }
    AfterEach{
        # test file will exist within the same context, so it needs to be blown away
        $doesInstallFileExist = Test-Path $installConfigPath
        $doesAzureFileExist = Test-Path $azureConfigPath
        if ($doesInstallFileExist)
        {
            Remove-Item $installConfigPath
        }
        if ($doesAzureFileExist)
        {
            Remove-Item $azureConfigPath
        }
    } 
    Context 'Migrating AAD Settings using Integration Tests'{
        InModuleScope Install-Identity-Utilities{
            It 'Should successfully run the migration'{
                # Act
                $runResult = Migrate-AADSettings -installConfigPath $installConfigPath -azureConfigPath $azureConfigPath -nodesToSearch $nodesToSearch
                
                # Assert
                $runResult | Should -Be $true
            }
            It 'Should not run the migration if tenant child node doesnt exist'{
               # Act
               # Remove tenants variable from install.config
               Remove-XMLChildNodes -azureConfigPath $installConfigPath -configSection "identity" -nodesToSearch "tenants" -childNodeGetAttribute "name" | Out-Null

               $runResult = Migrate-AADSettings -installConfigPath $installConfigPath -azureConfigPath $azureConfigPath -nodesToSearch $nodesToSearch
               
               # Assert
               $runResult | Should -Be $false
           }
           It 'Should succeed but not remove install.config settings if comparison files are different'{
               # Act
               # Add an additional variable in azureSecretName node for azuresettings.config
               $configSection = "identity"
               $nodeToSearch = "azureSecretName"
               $childNodeGetAttribute = "name"
               $azureSecretNode = Get-XMLChildNodes -installConfigPath $azureConfigPath -configSection $configSection -nodesToSearch $nodeToSearch -childNodeGetAttribute "name"
               $azureSecretNode[1].Attributes[0].value = "azureSecretSauce"
               $azureSecretNode[1].Attributes[1].value = "specialrecipe"
               Add-XMLChildNodes -azureConfigPath $azureConfigPath -configSection $configSection -childNodesInOrder $nodeToSearch -childNodesToAdd $azureSecretNode -skipDuplicateSearch | Out-Null

               $runResult = Migrate-AADSettings -installConfigPath $installConfigPath -azureConfigPath $azureConfigPath -nodesToSearch $nodesToSearch
               
               # Assert
               $runResult | Should -Be $true
               $childNodesNotDeleted = Get-XMLChildNodes -installConfigPath $installConfigPath -configSection $configSection -nodesToSearch $nodesToSearch -childNodeGetAttribute $childNodeGetAttribute
               $childNodesNotDeleted.Count | Should -Be 6
            }
        }
    }
    Remove-Variable testInstallFile -Scope Global
    Remove-Variable testAzureFile -Scope Global
    Remove-Variable testInstallFileLoc -Scope Global
    Remove-Variable testAzureFileLoc -Scope Global
    Remove-Variable installConfigPath -Scope Global
    Remove-Variable azureConfigPath -Scope Global
    Remove-Variable nodesToSearch -Scope Global
  }
}