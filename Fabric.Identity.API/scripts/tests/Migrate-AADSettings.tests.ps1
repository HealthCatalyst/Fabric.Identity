param(
    [string] $targetFilePath = "$PSScriptRoot\..\Install-Identity-Utilities.psm1"
)

Write-Host $targetFilePath
# Force re-import to pick up latest changes
Import-Module $targetFilePath -Force

# Need to use global variables in Pester when abstracting BeforeEach and AfterEach Setup Code
# $TestDrive is not accessible in a Global variable, only in the Describe BeforeEach and AfterEach
$Global:testInstallFile = 'install.config'
$Global:testAzureFile = 'testAzure.config'
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
        $dir = ".\"
        Set-Location $dir
        Get-Content "$dir\$testInstallFile" | Out-File $installConfigPath
        }
        if (!$doesAzureFileExist)
        {
        $dir = ".\"
        Set-Location $dir
        Get-Content "$dir\$testAzureFile" | Out-File $azureConfigPath
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
        $Global:installConfigPath = "$($TestDrive)\$($testInstallFile)"
        $Global:azureConfigPath = "$($TestDrive)\$($testAzureFile)"
        $doesInstallFileExist = Test-Path $installConfigPath
        $doesAzureFileExist = Test-Path $azureConfigPath
        if (!$doesInstallFileExist)
        {
        $dir = ".\"
        Set-Location $dir
        Get-Content "$dir\$testInstallFile" | Out-File $installConfigPath
        }
        if (!$doesAzureFileExist)
        {
        $dir = ".\"
        Set-Location $dir
        Get-Content "$dir\$testAzureFile" | Out-File $azureConfigPath
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
}
Remove-Variable testInstallFile -Scope Global
Remove-Variable testAzureFile -Scope Global
Remove-Variable installConfigPath -Scope Global
Remove-Variable azureConfigPath -Scope Global
Remove-Variable nodesToSearch -Scope Global

