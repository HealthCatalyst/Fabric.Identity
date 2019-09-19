# Fabric.Identity.API
A .Net Core 1.1 service that provides centralized authentication and authorization for health care applications and apis that want to participate in the Fabric ecosystem.

# Building and running
The Fabric Identity API depends on having the latest version of DOS installed properly as a prerequisite to running Fabric Identity API.

- In Fabric.Identity.API/appsettings.json, update the `HostingOptions.UseTestUsers` to be false
- Update `HostingOptions.StorageProvider` to be `SqlServer`
- Update `ConnectionStrings.IdentityDatabase` to be `Server=localhost` instead of Server=(localdb)\\mssqllocaldb
- Update `IdentityServerConfidentialClientSettings.Authority` to be `http://localhost/IdentityDev`
- Update `DiscoveryServiceEndpoint` setting to point at your installed version of DiscoveryService, e.g. `https://host.domain.local/DiscoveryService/v1`.
- Update `IdentityProviderSearchSettings.BaseUrl` setting to point at your installed version of Fabric.IdentityProviderSearchService, e.g. `https://host.domain.local/identity`. If you are debugging Fabric.IdentityProviderSearchService, then the debug version, `http://localhost/IdPSSDev`. Make sure to walk through the `Readme.md` for Fabric.IdentityProviderSearchService for the correct setup, then start debugging Fabric.IdentityProviderSearchService in Local IIS mode.

- Update `IdentityServerConfidentialClientSettings.ClientSecret` to be `secret`
- The `IdentityServerConfidentialClientSettings.ClientID` doesn't seem to be used when debugging and is just a placeholder
- Update the `Identity` database `ClientSecrets` table `Value` column. Look for `fabric-identity-client` ClientId, in Clients table.
  Using the Client Id find the correct row in `ClientSecrets.Value`. Uncomment and copy the following code, then run in powershell. Copy the returned secret value. Use a sql update script or right-click and edit the `ClientSecrets` table and paste in the secret value.
    <!--$fabricInstallerSecret = "secret"
    #$fabricInstallerSecret = [System.Convert]::ToBase64String([guid]::NewGuid().ToByteArray()).Substring(0,16)
    Write-Host "New Installer secret: $fabricInstallerSecret"
    $sha = [System.Security.Cryptography.SHA256]::Create()
    $hashedSecret = [System.Convert]::ToBase64String($sha.ComputeHash([System.Text.Encoding]::UTF8.GetBytes($fabricInstallerSecret)))
    #Invoke-Sql -connectionString $identityDbConnectionString -sql $query -parameters @{value=$hashedSecret} | Out-Null
    $fabricInstallerSecret
    $hashedSecret-->

- In the `Metadata` database (EDWAdmin) `CatalystAdmin.DiscoveryServiceBASE` table, change the `ServiceUrl` for ServiceNM `IdentityService` to be `http://localhost/IdentityDev`
- In VS 2017 ensure the startup project is `Fabric.Identity.API` and the debug profile is set to `IIS`.
- In VS 2017 begin debugging by pressing `F5`
- In IIS, a new web application will be created `IdentityDev`.  You will need to change the app pool; I suggest changing it to the same one as Identity.
In the new web application `IdentityDev` add the following `environmentVariable` to the web.config 
`<environmentVariable name="IdentityServerConfidentialClientSettings__ClientSecret" value="secret" />`

- From time to time, the debugger set to `IIS` will remove the https binding.  If you notice things not working, check that out.
Also, you will need to stop and start the `App Pools` for Identity and Authorization after changing settings in `Identity` database and `Metadata` (EDWAdmin) database tables, to make sure previous values aren't cached.

This will allow you to set breakpoints in the Fabric.Identity.API code via VS 2017.