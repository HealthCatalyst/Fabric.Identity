
CREATE ROLE [IdentityServiceRole];
GO

GRANT SELECT, INSERT, UPDATE, DELETE ON [dbo].[ApiClaims] TO IdentityServiceRole;
GO

GRANT SELECT, INSERT, UPDATE, DELETE ON [dbo].[ApiResources] TO IdentityServiceRole;
GO

GRANT SELECT, INSERT, UPDATE, DELETE ON [dbo].[ApiScopeClaims] TO IdentityServiceRole;
GO

GRANT SELECT, INSERT, UPDATE, DELETE ON [dbo].[ApiScopes] to IdentityServiceRole;
GO

GRANT SELECT, INSERT, UPDATE, DELETE ON [dbo].[ApiSecrets] to IdentityServiceRole;
GO

GRANT SELECT, INSERT, UPDATE, DELETE ON [dbo].[ClientClaims] to IdentityServiceRole;
GO

GRANT SELECT, INSERT, UPDATE, DELETE ON [dbo].[ClientCorsOrigins] TO IdentityServiceRole;
GO

GRANT SELECT, INSERT, UPDATE, DELETE ON [dbo].[ClientGrantTypes] to IdentityServiceRole;
GO

GRANT SELECT, INSERT, UPDATE, DELETE ON [dbo].[ClientIdPRestrictions] TO IdentityServiceRole;
GO

GRANT SELECT, INSERT, UPDATE, DELETE ON [dbo].[ClientPostLogoutRedirectUris] TO IdentityServiceRole;
GO

GRANT SELECT, INSERT, UPDATE, DELETE ON [dbo].[ClientRedirectUris] to IdentityServiceRole;
GO

GRANT SELECT, INSERT, UPDATE, DELETE ON [dbo].[Clients] to IdentityServiceRole;
GO

GRANT SELECT, INSERT, UPDATE, DELETE ON [dbo].[ClientScopes] to IdentityServiceRole;
GO

GRANT SELECT, INSERT, UPDATE, DELETE ON [dbo].[ClientSecrets] to IdentityServiceRole;
GO

GRANT SELECT, INSERT, UPDATE, DELETE ON [dbo].[IdentityClaims] to IdentityServiceRole;
GO

GRANT SELECT, INSERT, UPDATE, DELETE ON [dbo].[IdentityResources] to IdentityServiceRole;
GO

GRANT SELECT, INSERT, UPDATE, DELETE ON [dbo].[PersistedGrants] to IdentityServiceRole;
GO

GRANT SELECT, INSERT, UPDATE, DELETE ON [dbo].[UserClaims] to IdentityServiceRole;
GO

GRANT SELECT, INSERT, UPDATE, DELETE ON [dbo].[UserLogins] to IdentityServiceRole;
GO

GRANT SELECT, INSERT, UPDATE, DELETE ON [dbo].[Users] to IdentityServiceRole;
GO

GRANT SELECT, INSERT, UPDATE, DELETE ON [dbo].[EventLogs] to IdentityServiceRole;
GO