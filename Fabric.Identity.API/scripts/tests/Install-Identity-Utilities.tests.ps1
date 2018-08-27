param(
    [string] $targetFilePath = "$PSScriptRoot\..\Install-Identity-Utilities.psm1"
)

Write-Host $targetFilePath
# Force re-import to pick up latest changes
Import-Module $targetFilePath -Force
#Import-Module Pester -Force

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
            Mock -ModuleName Install-Identity-Utilities -CommandName Invoke-WebRequest -MockWith {}
            Install-DotNetCoreIfNeeded -version "1.1.1234.123" -downloadUrl "http://example.com"
            Assert-MockCalled -ModuleName Install-Identity-Utilities -CommandName Invoke-WebRequest -Times 0 -Exactly

        }
        InModuleScope Install-Identity-Utilities {
            It 'Should install proper version' {
                Mock -CommandName Test-PrerequisiteExact -MockWith { return $false }
                Mock -CommandName Invoke-WebRequest -MockWith {}
                Mock -CommandName Start-Process -MockWith {}
                Mock -CommandName Restart-W3SVC -MockWith {}
                Mock -CommandName Remove-Item -MockWith {}
                Install-DotNetCoreIfNeeded -version "1.1.1234.123" -downloadUrl "http://example.com"
                Assert-MockCalled -CommandName Invoke-WebRequest -Times 1 -Exactly
                Assert-MockCalled -CommandName Start-Process -Times 1 -Exactly
                Assert-MockCalled -CommandName Restart-W3SVC -Times 1 -Exactly
                Assert-MockCalled -CommandName Remove-Item -Times 1 -Exactly
            }

            It 'Should throw an exception if Start-Process throws' {
                Mock -CommandName Test-PrerequisiteExact -MockWith { return $false }
                Mock -CommandName Invoke-WebRequest -MockWith {}
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
                $selectedSite = Get-IISWebSiteForInstall -selectedSiteName "Default Web Site" -quiet $true
                $selectedSite.Name | Should -Be "Default Web Site"
            }
            It 'Should throw an exception if given site name does not exist' {
                {Get-IISWebSiteForInstall -selectedSiteName "Bad Site" -quiet $true} | Should -Throw
            }
        }
    }

    Context 'Interactive Mode - One Site' {
        InModuleScope Install-Identity-Utilities {
            It 'Should return the only web site when one web site is configured without prompting' {
                $defaultWebSite = New-Object -TypeName psobject -Property @{Name = "Default Web Site"; Id = 1}
                Mock -CommandName Get-ChildItem -MockWith { return $defaultWebSite }
                Mock -CommandName Read-Host -MockWith { }
                $selectedSite = Get-IISWebSiteForInstall -selectedSiteName "" -quiet $false
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
                $selectedSite = Get-IISWebSiteForInstall -selectedSiteName "" -quiet $false
                $selectedSite.Name | Should -Be "Test Site"
            }  
            
            It 'Should throw an exception when an invalid site is selected' {
                $defaultWebSite = New-Object -TypeName psobject -Property @{Name = "Default Web Site"; Id = 1}
                $testWebSite =  New-Object -TypeName psobject -Property @{Name = "Test Site"; Id = 2}
                Mock -ModuleName Install-Identity-Utilities -CommandName Get-ChildItem -MockWith { return @( $defaultWebSite, $testWebSite) }
                Mock -ModuleName Install-Identity-Utilities -CommandName Read-Host -MockWith { 3 }
                {Get-IISWebSiteForInstall -selectedSiteName "" -quiet $false } | Should -Throw
            }
        }
    }
}

Describe 'Get-Certificates' -Tag 'Unit'{
    Context 'Quiet Mode' {
        InModuleScope Install-Identity-Utilities {
            It 'Should return certificates without prompt'{
                Mock -ModuleName Install-Identity-Utilities -CommandName Test-ShouldShowCertMenu -MockWith { $false }
                Mock -ModuleName Install-Identity-Utilities -CommandName Get-Certificate -MockWith { return @{Thumbprint = 123456; Subject = "CN=HC2234.hqcatalyst.local"}}
                Mock -ModuleName Install-Identity-Utilities -CommandName Add-InstallationSetting -MockWith {}
                Mock -CommandName Read-Host -MockWith { }
                $certs = Get-Certificates -primarySigningCertificateThumbprint "123456" -encryptionCertificateThumbprint "123456" -quiet $true
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
                {Get-Certificates -primarySigningCertificateThumbprint "123456" -encryptionCertificateThumbprint "123456" -quiet $true } | Should -Throw
            }
        }
    }

    Context 'Interactive Mode'{
        InModuleScope Install-Identity-Utilities{
            It 'Should prompt and return certificates'{
                # Arrange
                $cert1 = New-Object -TypeName psobject -Property @{Thumbprint = 678901; Subject = "CN=HC2234.hqcatalyst.local"}
                $cert2 =  New-Object -TypeName psobject -Property @{Thumbprint = 123456; Subject = "CN=HC2234.hqcatalyst.local"}
                Mock -ModuleName Install-Identity-Utilities -CommandName Get-CertsFromLocation -MockWith { return @($cert1, $cert2)}
                Mock -ModuleName Install-Identity-Utilities -CommandName Test-ShouldShowCertMenu -MockWith { $true }
                Mock -ModuleName Install-Identity-Utilities -CommandName Get-Certificate -MockWith { return @{Thumbprint = 123456; Subject = "CN=HC2234.hqcatalyst.local"}}
                Mock -ModuleName Install-Identity-Utilities -CommandName Add-InstallationSetting -MockWith {}
                Mock -CommandName Read-Host -MockWith { 2 }

                # Act
                $certs = Get-Certificates -primarySigningCertificateThumbprint "123456" -encryptionCertificateThumbprint "123456" -quiet $true

                # Assert
                Assert-MockCalled -ModuleName Install-Identity-Utilities -CommandName Get-Certificate -Times 2 -Exactly
                Assert-MockCalled -ModuleName Install-Identity-Utilities -CommandName Add-InstallationSetting -Times 3 -Exactly
                Assert-MockCalled -ModuleName Install-Identity-Utilities -CommandName Read-Host -Times 1 -Exactly
                $certs.SigningCertificate.Thumbprint | Should -Be "123456"
                $certs.EncryptionCertificate.Thumbprint | Should -Be "123456"
            }

            It 'Should throw an exception if no selection is made'{
                # Arrange
                $cert1 = New-Object -TypeName psobject -Property @{Thumbprint = 678901; Subject = "CN=HC2234.hqcatalyst.local"}
                $cert2 =  New-Object -TypeName psobject -Property @{Thumbprint = 123456; Subject = "CN=HC2234.hqcatalyst.local"}
                Mock -ModuleName Install-Identity-Utilities -CommandName Get-CertsFromLocation -MockWith { return @($cert1, $cert2)}
                Mock -ModuleName Install-Identity-Utilities -CommandName Test-ShouldShowCertMenu -MockWith { $true }
                Mock -ModuleName Install-Identity-Utilities -CommandName Get-Certificate -MockWith { return @{Thumbprint = 123456; Subject = "CN=HC2234.hqcatalyst.local"}}
                Mock -ModuleName Install-Identity-Utilities -CommandName Add-InstallationSetting -MockWith {}
                Mock -CommandName Read-Host -MockWith { $null }

                # Act/Assert
                {Get-Certificates -primarySigningCertificateThumbprint "123456" -encryptionCertificateThumbprint "123456" -quiet $false} | Should -Throw
            }

            It 'Should throw an exception if a bad selection is made'{
                # Arrange
                $cert1 = New-Object -TypeName psobject -Property @{Thumbprint = 678901; Subject = "CN=HC2234.hqcatalyst.local"}
                $cert2 =  New-Object -TypeName psobject -Property @{Thumbprint = 123456; Subject = "CN=HC2234.hqcatalyst.local"}
                Mock -ModuleName Install-Identity-Utilities -CommandName Get-CertsFromLocation -MockWith { return @($cert1, $cert2)}
                Mock -ModuleName Install-Identity-Utilities -CommandName Test-ShouldShowCertMenu -MockWith { $true }
                Mock -ModuleName Install-Identity-Utilities -CommandName Get-Certificate -MockWith { return @{Thumbprint = 123456; Subject = "CN=HC2234.hqcatalyst.local"}}
                Mock -ModuleName Install-Identity-Utilities -CommandName Add-InstallationSetting -MockWith {}
                Mock -CommandName Read-Host -MockWith { 3 }

                # Act/Assert
                {Get-Certificates -primarySigningCertificateThumbprint "123456" -encryptionCertificateThumbprint "123456" -quiet $false} | Should -Throw
            }

            It 'Should throw an exception if certs cannot be retreived from certificate store'{
                # Arrange
                Mock -ModuleName Install-Identity-Utilities -CommandName Get-CertsFromLocation -MockWith { throw }
                Mock -ModuleName Install-Identity-Utilities -CommandName Test-ShouldShowCertMenu -MockWith { $true }

                # Act/Assert
                {Get-Certificates -primarySigningCertificateThumbprint "123456" -encryptionCertificateThumbprint "123456" -quiet $false} | Should -Throw
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
                # Act
                $iisUser = Get-IISAppPoolUser -appName "identity" -storedIisUser "fabric\test.user"

                # Assert
                $iisUser.UserName | Should -Be "fabric\test.user"
                Assert-MockCalled -ModuleName Install-Identity-Utilities -CommandName Read-Host -Times 0 -Exactly
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
                Mock -ModuleName Install-Identity-Utilities -CommandName Confirm-Credentials -MockWith { New-Object -TypeName "System.Management.Automation.PSCredential" -ArgumentList $userName, $password }

                # Act
                $iisUser = Get-IISAppPoolUser -appName "identity" -storedIisUser $userName

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
                { Get-IISAppPoolUser -appName "identity" -storedIisUser "fabric/test.user" } | Should -Throw
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
                $appInsightsKey = Get-AppInsightsKey -appInsightsInstrumentationKey "123456" -quiet $true

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
                $appInsightsKey = Get-AppInsightsKey -appInsightsInstrumentationKey "123456" -quiet $false

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
                $sqlServerAddress = Get-SqlServerAddress -sqlServerAddress "somemachine.fabric.local" -quiet $true

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
                $sqlServerAddress = Get-SqlServerAddress -sqlServerAddress "somemachine.fabric.local" -quiet $false

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
                $discoUrl = Get-DiscoveryServiceUrl -discoveryServiceUrl "https://host.fabric.local/DiscoveryService/v1" -quiet $true

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
                $discoUrl = Get-DiscoveryServiceUrl -discoveryServiceUrl "https://host.fabric.local/DiscoveryService/v1" -quiet $false

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
                $appEndpoint = Get-ApplicationEndpoint -appName "identity" -applicationEndpoint "https://host.fabric.local/identity" -quiet $true

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
                $appEndpoint = Get-ApplicationEndpoint -appName "identity" -applicationEndpoint "https://host.fabric.local/identity" -quiet $false

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
                $dbconnectionString = Get-IdentityDatabaseConnectionString -identityDbName "identity" -sqlServerAddress "host.fabric.local" -quiet $true

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
                {Get-IdentityDatabaseConnectionString -identityDbName "identity" -sqlServerAddress "host.fabric.local" -quiet $true } | Should -Throw
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
                $dbconnectionString = Get-IdentityDatabaseConnectionString -identityDbName "identity" -sqlServerAddress "host.fabric.local" -quiet $false

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
                $dbconnectionString = Get-MetadataDatabaseConnectionString -metadataDbName "EDWAdmin" -sqlServerAddress "host.fabric.local" -quiet $true

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
                {Get-MetadataDatabaseConnectionString -metadataDbName "EDWAdmin" -sqlServerAddress "host.fabric.local" -quiet $true } | Should -Throw
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
                $dbconnectionString = Get-MetadataDatabaseConnectionString -metadataDbName "EDWAdmin" -sqlServerAddress "host.fabric.local" -quiet $false

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

Describe 'Publish-Identity'{
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

                
                Mock -ModuleName Install-Identity-Utilities -CommandName New-AppRoot -MockWith {}
                Mock -ModuleName Install-Identity-Utilities -CommandName Test-AppPoolExistsAndRunsAsUser -MockWith { $true }
                Mock -ModuleName Install-Identity-Utilities -CommandName New-App -MockWith {}
                Mock -ModuleName Install-Identity-Utilities -CommandName Publish-WebSite -MockWith {}
                Mock -ModuleName Install-Identity-Utilities -CommandName New-AppPool -MockWith {}
                Mock -ModuleName Install-Identity-Utilities -CommandName Get-InstalledVersion -MockWith { $expectedVersion }

                # Act
                $publishedApp = Publish-Identity -site $site -appName "identity" -iisUser $iisUser -zipPackage "$env:Temp\identity.zip"

                # Assert
                $publishedApp.version = "1.4.12345"
                $publishedApp.applicationDirectory = "C:\inetpub\wwwroot\identity"
                Assert-MockCalled -ModuleName Install-Identity-Utilities -CommandName New-AppRoot -Times 1 -Exactly
                Assert-MockCalled -ModuleName Install-Identity-Utilities -CommandName New-App -Times 1 -Exactly
                Assert-MockCalled -ModuleName Install-Identity-Utilities -CommandName Publish-WebSite -Times 1 -Exactly
                Assert-MockCalled -ModuleName Install-Identity-Utilities -CommandName New-AppPool -Times 0 -Exactly

            }
        }
    }

    Context 'App pool does not exists'{
        InModuleScope Install-Identity-Utilities{
            It 'Should install app, create app pool and return version and directory'{
                # Arrange
                $userName = "fabric\test.user"
                $password = ConvertTo-SecureString "supersecretpassword" -AsPlainText -Force
                $credential = New-Object -TypeName "System.Management.Automation.PSCredential" -ArgumentList $userName, $password
                $expectedVersion = "1.4.12345"

                $site = @{Name="Default Web Site"; physicalPath = "C:\inetpub\wwwroot"}
                $iisUser = @{UserName = $userName; Credential = $credential }

                
                Mock -ModuleName Install-Identity-Utilities -CommandName New-AppRoot -MockWith {}
                Mock -ModuleName Install-Identity-Utilities -CommandName Test-AppPoolExistsAndRunsAsUser -MockWith { $false }
                Mock -ModuleName Install-Identity-Utilities -CommandName New-App -MockWith {}
                Mock -ModuleName Install-Identity-Utilities -CommandName Publish-WebSite -MockWith {}
                Mock -ModuleName Install-Identity-Utilities -CommandName New-AppPool -MockWith {}
                Mock -ModuleName Install-Identity-Utilities -CommandName Get-InstalledVersion -MockWith { $expectedVersion }

                # Act
                $publishedApp = Publish-Identity -site $site -appName "identity" -iisUser $iisUser -zipPackage "$env:Temp\identity.zip"

                # Assert
                $publishedApp.version = "1.4.12345"
                $publishedApp.applicationDirectory = "C:\inetpub\wwwroot\identity"
                Assert-MockCalled -ModuleName Install-Identity-Utilities -CommandName New-AppRoot -Times 1 -Exactly
                Assert-MockCalled -ModuleName Install-Identity-Utilities -CommandName New-App -Times 1 -Exactly
                Assert-MockCalled -ModuleName Install-Identity-Utilities -CommandName Publish-WebSite -Times 1 -Exactly
                Assert-MockCalled -ModuleName Install-Identity-Utilities -CommandName New-AppPool -Times 1 -Exactly

            }
        }
    }
}