# Need to use global variables in Pester when abstracting BeforeEach and AfterEach Setup Code
# $TestDrive is not accessible in a Global variable, only in the Describe BeforeEach and AfterEach
$Global:testInstallFile = 'testInstall.config'
$Global:testAzureFile = 'testAzure.config'
$Global:installConfigPath
$Global:azureConfigPath
$Global:localInstallConfigPath = "$PSSCriptRoot\install.config"
$Global:localAzureConfigPath = "$PSScriptRoot\azuresettings.config"
$Global:scriptParams

Describe 'Running Install-Identity-Discovery-IdPSS that calls Migrate-AADSettings' -Tag 'Integration'{
      # Arrange
      # For the Invoke-Pester Install-Identity-Discovery-IdPSS to work, the referenced modules
      # need to be located in the same folder where the test script is called.
      # Copy once before and remove after the tests have all run
      Copy-Item "..\Install-Identity-Utilities.psm1" 

      BeforeEach{
        # Arrange 
        # Add to the powershell TestDrive which cleans up after each context, leaving the tests folder configs unchanged
        $Global:installConfigPath = "$($TestDrive)\$($testInstallFile)"
        $Global:azureConfigPath = "$($TestDrive)\$($testAzureFile)"
        $Global:scriptParams = @{azureConfigPath = $localAzureConfigPath; installConfigPath = $localInstallConfigPath; migrationInstallConfigPath = $installConfigPath; migrationAzureConfigPath = $azureConfigPath; quiet = $true; test = $true}
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
        $doesNewAzureFileExist = Test-Path ".\azuresettings.config"
        if ($doesNewAzureFileExist)
        {
            Remove-Item ".\azuresettings.config"
        }
        Clear-Content ".\DosInstall.log"
    } 
    Context 'Migrating AAD Settings using Integration Tests'{
        It 'Should Successfully run the migration'{
            # Act
            ..\Install-Identity-Discovery-IdPSS.ps1 @scriptParams
           
            # Assert
            $completingWordsToFind = "Completed the Migration of AAD Settings"
            $file = Get-Content -Path ".\DosInstall.log"
            $hasCompletingWords = $file | Where-Object{$_ -match $completingWordsToFind}
            if($hasCompletingWords)
            {
               Write-DosMessage -Level "Information" -Message "The migration ran to completion"
            }
            else 
            {
               Write-DosMessage -Level "Error" -Message "The migration should have been successful" 
            }

            $doesAzureFileExist = Test-Path "$PSScriptRoot\azuresettings.config"
            if($doesAzureFileExist)
            {
               Write-DosMessage -Level "Information" -Message "The azuresettings.config file was created successfully"
            }
            else 
            {
               Write-DosMessage -Level "Error" -Message "The azuresettings.config file should have been created"
            }
        }
        It 'Should not run without install.config file'{
          # Arrange
          $wrongInstallConfig = "noinstall.config"
          $Global:scriptParams = @{azureConfigPath = $localAzureConfigPath; installConfigPath = $localInstallConfigPath; migrationInstallConfigPath = "$($TestDrive)\$($wrongInstallConfig)"; migrationAzureConfigPath = $azureConfigPath; quiet = $true; test = $true}
          
          # Act
          ..\Install-Identity-Discovery-IdPSS.ps1 @scriptParams
          
          # Assert
          $completingWordsToFind = "Completed the Migration of AAD Settings"
          $file = Get-Content -Path ".\DosInstall.log"
          $hasCompletingWords = $file | Where-Object{$_ -match $completingWordsToFind}
          if($null -eq $hasCompletingWords)
          {
             Write-DosMessage -Level Information -Message "The migration was not run"
          }
          else 
          {
             Write-DosMessage -Level "Error" -Message "migration should have failed"
             Write-DosMessage -Level "Error" -Message "$wrongInstallConfig should not exist, check the name of the current config file"
          }
        }
        It 'Should not run without install.config permissions'{
          # Arrange
          $currentInstallFile = "testInstall.config"
          $noPermissionFile = "nopermission.config"
          $testDrivePath = "$($TestDrive)\$($currentInstallFile)"
          $doesInstallFileExist = Test-Path $testDrivePath
          if ($doesInstallFileExist)
          {
            # Have to set the acl on a file with no permissions and then set it back
            # You cannot check in a file with no permissions visual studio will show an error
            $returnNoPermissionsAcl = Deny-FilePermissions -filePath ".\$noPermissionFile"
            #Apply Changes    
            Set-Acl "$($TestDrive)\$($currentInstallFile)" $returnNoPermissionsAcl
          }

          # Act
           ..\Install-Identity-Discovery-IdPSS.ps1 @scriptParams
         
          # Assert
          $completingWordsToFind = "Completed the Migration of AAD Settings"
          $file = Get-Content -Path ".\DosInstall.log"
          $hasCompletingWords = $file | Where-Object{$_ -match $completingWordsToFind}
          if($null -eq $hasCompletingWords)
          {
             Write-DosMessage -Level "Information" -Message "The migration was not run"
          }
          else 
          {
             Write-DosMessage -Level "Error" -Message "migration should have failed"
             Write-DosMessage -Level "Error" -Message "Permissions should have been denied on $currentInstallFile"
          }

          # Remove the Everyone permission added to the file
          Remove-FilePermissions -filePath ".\$noPermissionFile"
        }
        It 'Should not run with malformed xml in install.config'{
          # Arrange
          $malformedXMLFile = "testInstallMalformed.config"
          $Global:installConfigPath = "$($TestDrive)\$($malformedXMLFile)"
          Get-Content "$dir\$malformedXMLFile" | Out-File $installConfigPath
          $Global:scriptParams = @{azureConfigPath = $localAzureConfigPath; installConfigPath = $localInstallConfigPath; migrationInstallConfigPath = $installConfigPath; migrationAzureConfigPath = $azureConfigPath; quiet = $true; test = $true}
          
          ..\Install-Identity-Discovery-IdPSS.ps1 @scriptParams
         
          # Assert
          $completingWordsToFind = "Completed the Migration of AAD Settings"
          $file = Get-Content -Path ".\DosInstall.log"
          $hasCompletingWords = $file | Where-Object{$_ -match $completingWordsToFind}
          if($null -eq $hasCompletingWords)
          {
             Write-DosMessage -Level "Information" -Message "The migration was not completed"
          }
          else 
          {
             Write-DosMessage -Level "Error" -Message "migration should have failed"
             Write-DosMessage -Level "Error" -Message "There should be Invalid XML in the $testInstallFile"
          }
        }
    }

  Remove-Item ".\Fabric-Install-Utilities.psm1" 
  Remove-Item ".\Install-Identity-Utilities.psm1" 
}
Remove-Variable testInstallFile -Scope Global
Remove-Variable testAzureFile -Scope Global
Remove-Variable installConfigPath -Scope Global
Remove-Variable azureConfigPath -Scope Global
Remove-Variable localInstallConfigPath -Scope Global
Remove-Variable localAzureConfigPath -Scope Global
Remove-Variable scriptParams -Scope Global
Remove-Module -Name Fabric-Install-Utilities
Remove-Module -Name Install-Identity-Utilities


