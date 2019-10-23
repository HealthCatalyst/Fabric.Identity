using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fabric.Identity.API.Infrastructure;
using Fabric.Identity.API.Models;
using Novell.Directory.Ldap;
using Polly.CircuitBreaker;
using Serilog;

namespace Fabric.Identity.API.Services
{
    public class LdapProviderService : IExternalIdentityProviderService
    {
        private readonly ILdapConnectionProvider _ldapConnectionProvider;
        private readonly ILogger _logger;
        private const string DomainKey = "DC=";
        private readonly PolicyProvider _policyProvider;

        public LdapProviderService(ILdapConnectionProvider ldapConnectionProvider, ILogger logger, PolicyProvider policyProvider)
        {
            _ldapConnectionProvider = ldapConnectionProvider;
            _logger = logger;
            _policyProvider = policyProvider;
        }
        public Task<FabricPrincipal> FindUserBySubjectIdAsync(string subjectId)
        {
            if (!subjectId.Contains(@"\"))
            {
                _logger.Information("subjectId '{subjectId}' was not in the correct format. Expected DOMAIN\\username.", subjectId);
                return Task.FromResult(new FabricPrincipal());
            }
            _logger.Debug("Searching LDAP for '{subjectId}'.", subjectId);
            var subjectIdParts = subjectId.Split('\\');
            var accountName = subjectIdParts[subjectIdParts.Length - 1];
            var ldapQuery = $"(&(objectClass=user)(objectCategory=person)(sAMAccountName={accountName}))";
            var users = SearchLdapSafe(ldapQuery);
            return Task.FromResult(users.FirstOrDefault());
        }

        public ICollection<FabricPrincipal> SearchUsers(string searchText)
        {
            var ldapQuery = $"(&(objectClass=user)(objectCategory=person)(|(sAMAccountName={searchText}*)(givenName={searchText}*)(sn={searchText}*)))";
            return SearchLdapSafe(ldapQuery);
        }

        private ICollection<FabricPrincipal> SearchLdapSafe(string ldapQuery)
        {
            //use a circuit breaker here so we don't consistently try to hit a down/unreachable service
            var users = new List<FabricPrincipal>();
            try
            {
                users = _policyProvider.LdapErrorPolicy.Execute(() => SearchLdap(ldapQuery)).ToList();
            }
            catch (LdapException ex)
            {
                // catch and log the error so we degrade gracefully when we can't connect to LDAP
                _logger.Error(ex, "LDAP Error when attempting to query LDAP with search: {searchText}", ldapQuery);
            }
            catch (BrokenCircuitException ex)
            {
                // catch and log the error so we degrade gracefully when we can't connect to LDAP
                _logger.Error(ex, "LdapProviderService circuit breaker is in an open state, not attempting to connect to LDAP", ldapQuery);
            }
            return users;
        }

        private ICollection<FabricPrincipal> SearchLdap(string ldapQuery)
        {
            var users = new List<FabricPrincipal>();
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
                        var user = new FabricPrincipal
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
