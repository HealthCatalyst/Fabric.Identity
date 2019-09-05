param(
    [string] $targetFilePath = "$PSScriptRoot\..\Install-IdPSS-Utilities.psm1"
)

# Force re-import to pick up latest changes
Import-Module $targetFilePath -Force
$directoryPath = [System.IO.Path]::GetDirectoryName($targetFilePath)
$identityUtilitiesPath = Join-Path -Path $directoryPath -ChildPath "/Install-Identity-Utilities.psm1"
Import-Module $identityUtilitiesPath -Force


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