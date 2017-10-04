using System;
using System.Collections.Generic;
using System.Linq;
using Fabric.Identity.API.Models;
using Novell.Directory.Ldap;
using Serilog;

namespace Fabric.Identity.API.Services
{
    public class LdapProviderService : IExternalIdentityProviderService
    {
        private readonly ILdapConnectionProvider _ldapConnectionProvider;
        private readonly ILogger _logger;
        public LdapProviderService(ILdapConnectionProvider ldapConnectionProvider, ILogger logger)
        {
            _ldapConnectionProvider = ldapConnectionProvider;
            _logger = logger;
        }
        public ExternalUser FindUserBySubjectId(string subjectId)
        {
            if (!subjectId.Contains(@"\"))
            {
                _logger.Information("subjectId '{subjectId}' was not in the correct format. Expected DOMAIN\\username.");
                return new ExternalUser();
            }
            var subjectIdParts = subjectId.Split('\\');
            var accountName = subjectIdParts[subjectIdParts.Length - 1];
            var ldapQuery = $"(&(objectClass=user)(objectCategory=person)(sAMAccountName={accountName}))";
            var users = SearchLdap(ldapQuery);
            return users.FirstOrDefault();
        }

        public ICollection<ExternalUser> SearchUsers(string searchText)
        {
            var ldapQuery = $"(&(objectClass=user)(objectCategory=person)(|(sAMAccountName={searchText}*)(givenName={searchText}*)(sn={searchText}*)))";
            return SearchLdap(ldapQuery);
        }

        private ICollection<ExternalUser> SearchLdap(string ldapQuery)
        {
            var users = new List<ExternalUser>();
            using (var ldapConnection = _ldapConnectionProvider.GetConnection())
            {
                if (ldapConnection == null)
                {
                    return users;
                }
                var results = ldapConnection.Search(_ldapConnectionProvider.BaseDn, LdapConnection.SCOPE_SUB, ldapQuery, null, false);
                while (results.hasMore())
                {
                    var next = results.next();
                    var atttributes = next.getAttributeSet();
                    var user = new ExternalUser
                    {
                        LastName = atttributes.getAttribute("SN") == null ? string.Empty : atttributes.getAttribute("SN").StringValue,
                        FirstName = atttributes.getAttribute("GIVENNAME") == null ? string.Empty : atttributes.getAttribute("GIVENNAME").StringValue,
                        MiddleName = atttributes.getAttribute("MIDDLENAME") == null ? string.Empty : atttributes.getAttribute("MIDDLENAME").StringValue,
                        SubjectId = GetSubjectId(atttributes.getAttribute("SAMACCOUNTNAME")?.StringValue, next.DN)
                    };
                    users.Add(user);
                }
                return users;
            }
        }

        private string GetSubjectId(string samAccountName, string distinguishedName)
        {
            if (string.IsNullOrEmpty(samAccountName))
            {
                return string.Empty;
            }

            if (string.IsNullOrEmpty(distinguishedName))
            {
                return samAccountName;
            }

            var distinguishedNameSegments = distinguishedName.Split(',');
            var topLevelSubDomain =
                distinguishedNameSegments.FirstOrDefault(s => s.StartsWith("DC=", StringComparison.OrdinalIgnoreCase));

            if (string.IsNullOrEmpty(topLevelSubDomain))
            {
                return samAccountName;
            }

            var netbiosName = topLevelSubDomain.ToUpperInvariant().Replace("DC=", string.Empty);
            if (netbiosName.Length > 15)
            {
                netbiosName = netbiosName.Substring(0, 15);
            }
            return $"{netbiosName}\\{samAccountName}";
        }
    }
}
