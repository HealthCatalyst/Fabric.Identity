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
        private const string DomainKey = "DC=";

        public LdapProviderService(ILdapConnectionProvider ldapConnectionProvider, ILogger logger)
        {
            _ldapConnectionProvider = ldapConnectionProvider;
            _logger = logger;
        }
        public ExternalUser FindUserBySubjectId(string subjectId)
        {
            if (!subjectId.Contains(@"\"))
            {
                _logger.Information("subjectId '{subjectId}' was not in the correct format. Expected DOMAIN\\username.", subjectId);
                return new ExternalUser();
            }
            _logger.Debug("Searching LDAP for '{subjectId}'.", subjectId);
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
                    _logger.Warning("Could not get an LDAP connection.");
                    return users;
                }
                _logger.Debug("Searching LDAP with query: {ldapQuery} and BaseDN: {baseDn}.", ldapQuery, _ldapConnectionProvider.BaseDn);
                var results = ldapConnection.Search(_ldapConnectionProvider.BaseDn, LdapConnection.SCOPE_SUB, ldapQuery, null, false);
                while (results.hasMore())
                {
                    try
                    {
                        var next = results.next();
                        _logger.Debug("Found entry with DN: {DN}", next.DN);
                        var attributeSet = next.getAttributeSet();
                        var user = new ExternalUser
                        {
                            LastName = attributeSet.getAttribute("SN") == null
                                ? string.Empty
                                : attributeSet.getAttribute("SN").StringValue,
                            FirstName = attributeSet.getAttribute("GIVENNAME") == null
                                ? string.Empty
                                : attributeSet.getAttribute("GIVENNAME").StringValue,
                            MiddleName = attributeSet.getAttribute("MIDDLENAME") == null
                                ? string.Empty
                                : attributeSet.getAttribute("MIDDLENAME").StringValue,
                            SubjectId = GetSubjectId(attributeSet.getAttribute("SAMACCOUNTNAME")?.StringValue, next.DN)
                        };
                        users.Add(user);
                        _logger.Debug("User: {@user}", user);
                    }
                    catch (LdapReferralException ex)
                    {
                        //log error but don't throw as this is not a fatal error.
                        _logger.Debug(ex, "Error querying LDAP, referral exception: {failedReferral}, {@data}", ex.FailedReferral, ex.Data);
                    }
                }
                return users;
            }
        }

        private string GetSubjectId(string samAccountName, string distinguishedName)
        {
            if (string.IsNullOrWhiteSpace(samAccountName))
            {
                return string.Empty;
            }

            if (string.IsNullOrWhiteSpace(distinguishedName))
            {
                return samAccountName;
            }

            var distinguishedNameSegments = distinguishedName.Split(',');
            var topLevelSubDomain =
                distinguishedNameSegments.FirstOrDefault(s => s.StartsWith(DomainKey, StringComparison.OrdinalIgnoreCase));

            if (string.IsNullOrWhiteSpace(topLevelSubDomain))
            {
                return samAccountName;
            }

            var netbiosName = topLevelSubDomain.ToUpperInvariant().Replace(DomainKey, string.Empty);
            if (netbiosName.Length > 15)
            {
                netbiosName = netbiosName.Substring(0, 15);
            }
            return $"{netbiosName}\\{samAccountName}";
        }
    }
}
