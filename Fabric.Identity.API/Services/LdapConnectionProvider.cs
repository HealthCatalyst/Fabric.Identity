using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fabric.Identity.API.Configuration;
using Novell.Directory.Ldap;

namespace Fabric.Identity.API.Services
{
    public class LdapConnectionProvider : ILdapConnectionProvider
    {
        private readonly LdapSettings _ldapSettings;

        public string BaseDn => _ldapSettings.BaseDn;

        public LdapConnectionProvider(LdapSettings ldapSettings)
        {
            _ldapSettings = ldapSettings ?? throw new ArgumentNullException(nameof(ldapSettings));
        }

        public ILdapConnection GetConnection()
        {
            var connection = new LdapConnection() { SecureSocketLayer = _ldapSettings.UseSsl };
            connection.Connect(_ldapSettings.Server, _ldapSettings.Port);
            connection.Bind(_ldapSettings.Username, _ldapSettings.Password);
            return connection;
        }
        
    }
}
