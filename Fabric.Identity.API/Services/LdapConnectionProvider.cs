using System;
using Fabric.Identity.API.Configuration;
using Novell.Directory.Ldap;
using Serilog;

namespace Fabric.Identity.API.Services
{
    public class LdapConnectionProvider : ILdapConnectionProvider
    {
        private readonly LdapSettings _ldapSettings;
        private readonly ILogger _logger;

        public string BaseDn => _ldapSettings.BaseDn;

        public LdapConnectionProvider(LdapSettings ldapSettings, ILogger logger)
        {
            _ldapSettings = ldapSettings ?? throw new ArgumentNullException(nameof(ldapSettings));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public ILdapConnection GetConnection()
        {
            if (!HasConnectionInfo())
            {
                return null;
            }
            var constraints = new LdapConstraints { ReferralFollowing = true };
            var connection = new LdapConnection() { SecureSocketLayer = _ldapSettings.UseSsl, Constraints = constraints };
            connection.Connect(_ldapSettings.Server, _ldapSettings.Port);
            connection.Bind(_ldapSettings.Username, _ldapSettings.Password);
            return connection;
        }

        private bool HasConnectionInfo()
        {
            if (string.IsNullOrEmpty(_ldapSettings.Server) || string.IsNullOrEmpty(_ldapSettings.Username) ||
                string.IsNullOrEmpty(_ldapSettings.Password))
            {
                _logger.Warning("LDAP Connection information is not specified in configuration, LDAP integration is disabled.");
                return false;
            }
            return true;
        }
        
    }
}
