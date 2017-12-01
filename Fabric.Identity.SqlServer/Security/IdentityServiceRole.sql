
CREATE ROLE [IdentityServiceRole];
GO

GRANT SELECT, INSERT, UPDATE ON [dbo].[ApiClaims] TO IdentityServiceRole;
GO

GRANT SELECT, INSERT, UPDATE ON [dbo].[ApiResources] TO IdentityServiceRole;
GO

GRANT SELECT, INSERT, UPDATE ON [dbo].[ApiScopeClaims] TO IdentityServiceRole;
GO

GRANT SELECT, INSERT, UPDATE ON [dbo].[ApiScopes] to IdentityServiceRole;
GO

GRANT SELECT, INSERT, UPDATE ON [dbo].[ApiSecrets] to IdentityServiceRole;
GO

GRANT SELECT, INSERT, UPDATE ON [dbo].[ClientClaims] to IdentityServiceRole;
GO

GRANT SELECT, INSERT, UPDATE ON [dbo].[ClientCorsOrigins] TO IdentityServiceRole;
GO

GRANT SELECT, INSERT, UPDATE, DELETE ON [dbo].[ClientGrantTypes] to IdentityServiceRole;
GO

GRANT SELECT, INSERT, UPDATE ON [dbo].[ClientIdPRestrictions] TO IdentityServiceRole;
GO

GRANT SELECT, INSERT, UPDATE ON [dbo].[ClientPostLogoutRedirectUris] TO IdentityServiceRole;
GO

GRANT SELECT, INSERT, UPDATE ON [dbo].[ClientRedirectUris] to IdentityServiceRole;
GO

GRANT SELECT, INSERT, UPDATE ON [dbo].[Clients] to IdentityServiceRole;
GO

GRANT SELECT, INSERT, UPDATE ON [dbo].[ClientScopes] to IdentityServiceRole;
GO

GRANT SELECT, INSERT, UPDATE ON [dbo].[ClientSecrets] to IdentityServiceRole;
GO

GRANT SELECT, INSERT, UPDATE ON [dbo].[IdentityClaims] to IdentityServiceRole;
GO

GRANT SELECT, INSERT, UPDATE ON [dbo].[IdentityResources] to IdentityServiceRole;
GO

GRANT SELECT, INSERT, UPDATE, DELETE ON [dbo].[PersistedGrants] to IdentityServiceRole;
GO

GRANT SELECT, INSERT, UPDATE, DELETE ON [dbo].[UserClaims] to IdentityServiceRole;
GO

GRANT SELECT, INSERT, UPDATE ON [dbo].[UserLogins] to IdentityServiceRole;
GO

GRANT SELECT, INSERT, UPDATE ON [dbo].[Users] to IdentityServiceRole;
GO