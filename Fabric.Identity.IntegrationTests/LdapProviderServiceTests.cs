using System;
using System.Collections.Generic;
using System.Linq;
using Fabric.Identity.API.Configuration;
using Fabric.Identity.API.Services;
using Moq;
using Novell.Directory.Ldap;
using Serilog;
using Xunit;

namespace Fabric.Identity.IntegrationTests
{
    public class LdapProviderServiceTests
    {
        [Fact]
        public void FindUser_Succeeds_WhenUserExists()
        {
            var logger = new Mock<ILogger>().Object;
            var settings = GetLdapSettings();

            var ldapConnectionProvider = new LdapConnectionProvider(settings, logger);
            var newUser = CreateTestUser("test", "user", settings.BaseDn, ldapConnectionProvider);
            var ldapProviderService = new LdapProviderService(ldapConnectionProvider, logger);
            var externalUser = ldapProviderService.FindUserBySubjectId($"TEST\\{newUser.getAttribute("cn").StringValue}");
            RemoveEntry(newUser, ldapConnectionProvider);
            Assert.NotNull(externalUser);
            Assert.Equal("test", externalUser.FirstName);
            Assert.Equal("user", externalUser.LastName);
            Assert.Equal("test.user", externalUser.Username);
            Assert.Equal("", externalUser.MiddleName);
        }

        [Fact]
        public void FindUser_ReturnsNull_WhenUserDoesNotExist()
        {
            var logger = new Mock<ILogger>().Object;
            var settings = GetLdapSettings();

            var ldapConnectionProvider = new LdapConnectionProvider(settings, logger);
            var ldapProviderService = new LdapProviderService(ldapConnectionProvider, logger);
            var externalUser = ldapProviderService.FindUserBySubjectId($"TEST\\nonexistant.user");
            Assert.Null(externalUser);
        }

        [Fact]
        public void FindUser_ReturnsNull_WithNoConnection()
        {
            var logger = new Mock<ILogger>().Object;
            var settings = new LdapSettings
            {
                Server = "",
                Port = 389,
                Username = @"",
                Password = "",
                UseSsl = false
            };

            var ldapConnectionProvider = new LdapConnectionProvider(settings, logger);
            var ldapProviderService = new LdapProviderService(ldapConnectionProvider, logger);
            var externalUser = ldapProviderService.FindUserBySubjectId($"TEST\\nonexistant.user");
            Assert.Null(externalUser);
        }

        [Theory]
        [MemberData(nameof(SearchData))]
        public void SearchUsers_Succeeds(string searchText, int count)
        {
            var logger = new Mock<ILogger>().Object;
            var settings = GetLdapSettings();

            var testUsers = new List<Tuple<string, string>>
            {
                Tuple.Create("mike", "trout"),
                Tuple.Create("mike", "piazza"),
                Tuple.Create("mike", "stanton"),
                Tuple.Create("carlos", "beltran")
            };

            var ldapConnectionProvider = new LdapConnectionProvider(settings, logger);
            var ldapEntries = CreateTestUsers(testUsers, settings.BaseDn, ldapConnectionProvider);

            var ldapProviderService = new LdapProviderService(ldapConnectionProvider, logger);
            var searchResults = ldapProviderService.SearchUsers(searchText);
            RemoveEntries(ldapEntries, ldapConnectionProvider);

            Assert.NotNull(searchResults);
            Assert.Equal(count, searchResults.Count);

        }

        public static IEnumerable<object[]> SearchData => new[]
        {
            new object[] {"mike", 3},
            new object[] {"mike.piazza", 1},
            new object[] {"car", 1},
            new object[] {"belt", 1},
            new object[] {"griffey", 0},
        };

        private void RemoveEntry(LdapEntry ldapEntry, LdapConnectionProvider ldapConnectionProvider)
        {
            using (var connection = ldapConnectionProvider.GetConnection())
            {
                connection.Delete(ldapEntry.DN);
            }
        }

        private void RemoveEntries(IEnumerable<LdapEntry> ldapEntries, LdapConnectionProvider ldapConnectionProvider)
        {
            foreach (var ldapEntry in ldapEntries)
            {
                RemoveEntry(ldapEntry, ldapConnectionProvider);
            }
        }

        private LdapEntry CreateTestUser(string firstName, string lastName, string baseDn, LdapConnectionProvider ldapConnectionProvider)
        {
            using (var connection = ldapConnectionProvider.GetConnection())
            {
                var ldapEntry = CreateNewLdapEntry(firstName, lastName, baseDn);
                connection.Add(ldapEntry);
                return ldapEntry;
            }
        }

        private List<LdapEntry> CreateTestUsers(IEnumerable<Tuple<string, string>> testUsers, string baseDn, LdapConnectionProvider ldapConnectionProvider)
        {
            var ldapEntries = new List<LdapEntry>();
            foreach (var testUser in testUsers)
            {
                ldapEntries.Add(CreateTestUser(testUser.Item1, testUser.Item2, baseDn, ldapConnectionProvider));
            }
            return ldapEntries;
        }

        private LdapEntry CreateNewLdapEntry(string firstName, string lastName, string baseDn)
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

        private LdapSettings GetLdapSettings()
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
