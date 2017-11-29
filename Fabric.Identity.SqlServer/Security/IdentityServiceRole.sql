IF NOT EXISTS(SELECT name FROM sys.sysusers WHERE name = 'IdentityServiceRole')
BEGIN
CREATE ROLE [IdentityServiceRole];
END


GRANT SELECT, INSERT, UPDATE ON [Identity].[ApiClaims] TO IdentityServiceRole;
GO

GRANT SELECT, INSERT, UPDATE ON [Identity].[ApiResources] TO IdentityServiceRole;
GO

GRANT SELECT, INSERT, UPDATE ON [Identity].[ApiScopeClaims] TO IdentityServiceRole;
GO

GRANT SELECT, INSERT, UPDATE ON [Identity].[ApiScopes] to IdentityServiceRole;
GO

GRANT SELECT, INSERT, UPDATE ON [Identity].[ApiSecrets] to IdentityServiceRole;
GO

GRANT SELECT, INSERT, UPDATE ON [Identity].[ClientClaims] to IdentityServiceRole;
GO

GRANT SELECT, INSERT, UPDATE ON [Identity].[ClientCorsOrigins] TO IdentityServiceRole;
GO

GRANT SELECT, INSERT, UPDATE, DELETE ON [Identity].[ClientGrantTypes] to IdentityServiceRole;
GO

GRANT SELECT, INSERT, UPDATE ON [Identity].[ClientIdPRestrictions] TO IdentityServiceRole;
GO

GRANT SELECT, INSERT, UPDATE ON [Identity].[ClientPostLogoutRedirectUris] TO IdentityServiceRole;
GO

GRANT SELECT, INSERT, UPDATE ON [Identity].[ClientRedirectUris] to IdentityServiceRole;
GO

GRANT SELECT, INSERT, UPDATE ON [Identity].[Clients] to IdentityServiceRole;
GO

GRANT SELECT, INSERT, UPDATE ON [Identity].[ClientScopes] to IdentityServiceRole;
GO

GRANT SELECT, INSERT, UPDATE ON [Identity].[ClientSecrets] to IdentityServiceRole;
GO

GRANT SELECT, INSERT, UPDATE ON [Identity].[IdentityClaims] to IdentityServiceRole;
GO

GRANT SELECT, INSERT, UPDATE ON [Identity].[IdentityResources] to IdentityServiceRole;
GO

GRANT SELECT, INSERT, UPDATE, DELETE ON [Identity].[PersistedGrants] to IdentityServiceRole;
GO

GRANT SELECT, INSERT, UPDATE ON [Identity].[UserClaims] to IdentityServiceRole;
GO

GRANT SELECT, INSERT, UPDATE ON [Identity].[UserLogins] to IdentityServiceRole;
GO

GRANT SELECT, INSERT, UPDATE ON [Identity].[Users] to IdentityServiceRole;
GO