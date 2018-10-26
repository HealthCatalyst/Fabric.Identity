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