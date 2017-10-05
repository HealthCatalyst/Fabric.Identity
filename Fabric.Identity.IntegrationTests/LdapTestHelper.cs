using System;
using System.Collections.Generic;
using System.Linq;
using Fabric.Identity.API.Configuration;
using Fabric.Identity.API.Services;
using Novell.Directory.Ldap;

namespace Fabric.Identity.IntegrationTests
{
    public static class LdapTestHelper
    {
        public static void RemoveEntry(LdapEntry ldapEntry, LdapConnectionProvider ldapConnectionProvider)
        {
            using (var connection = ldapConnectionProvider.GetConnection())
            {
                connection.Delete(ldapEntry.DN);
            }
        }

        public static void RemoveEntries(IEnumerable<LdapEntry> ldapEntries, LdapConnectionProvider ldapConnectionProvider)
        {
            foreach (var ldapEntry in ldapEntries)
            {
                RemoveEntry(ldapEntry, ldapConnectionProvider);
            }
        }

        public static LdapEntry CreateTestUser(string firstName, string lastName, string baseDn, LdapConnectionProvider ldapConnectionProvider)
        {
            using (var connection = ldapConnectionProvider.GetConnection())
            {
                var ldapEntry = CreateNewLdapEntry(firstName, lastName, baseDn);
                connection.Add(ldapEntry);
                return ldapEntry;
            }
        }

        public static List<LdapEntry> CreateTestUsers(IEnumerable<Tuple<string, string>> testUsers, string baseDn, LdapConnectionProvider ldapConnectionProvider)
        {
            return testUsers.Select(testUser => CreateTestUser(testUser.Item1, testUser.Item2, baseDn, ldapConnectionProvider)).ToList();
        }

        public static LdapEntry CreateNewLdapEntry(string firstName, string lastName, string baseDn)
        {
            var attributeSet = new LdapAttributeSet
            {
                new LdapAttribute("cn", $"{firstName}.{lastName}"),
                new LdapAttribute("objectClass", "user"),
                new LdapAttribute("objectCategory", "person"),
                new LdapAttribute("sAMAccountName", $"{firstName}.{lastName}"),
                new LdapAttribute("givenName", firstName),
                new LdapAttribute("sn", lastName)
            };
            var dn = $"cn={firstName}.{lastName},{baseDn}";
            return new LdapEntry(dn, attributeSet);
        }

        public static LdapSettings GetLdapSettings()
        {
            var settings = new LdapSettings
            {
                Server = "localhost",
                Port = 389,
                Username = @"cn=admin,dc=example,dc=org",
                Password = "password",
                UseSsl = false,
                BaseDn = "dc=example,dc=org"
            };

            var ldapServer = Environment.GetEnvironmentVariable("LDAPSETTINGS__SERVER");

            if (!string.IsNullOrEmpty(ldapServer))
            {
                settings.Server = ldapServer;
            }

            return settings;
        }

    }
}
