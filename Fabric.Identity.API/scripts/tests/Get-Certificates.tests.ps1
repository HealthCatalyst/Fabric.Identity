param(
    [string] $targetFilePath = "$PSScriptRoot\..\Install-Identity-Utilities.psm1"
)

Write-Host $targetFilePath
# Force re-import to pick up latest changes
Import-Module $targetFilePath -Force

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
                $cert1 = New-Object -TypeName psobject -Property @{Thumbprint = 678901; Subject = "CN=server.domain.local"}
                $cert2 =  New-Object -TypeName psobject -Property @{Thumbprint = 123456; Subject = "CN=server.domain.local"}
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
