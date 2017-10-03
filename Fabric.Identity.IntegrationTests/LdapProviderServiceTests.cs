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
            var settings = new LdapSettings
            {
                Server = "localhost",
                Port = 389,
                Username = @"cn=admin,dc=example,dc=org",
                Password = "password",
                UseSsl = false,
                BaseDn = "dc=example,dc=org"
            };

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
            var settings = new LdapSettings
            {
                Server = "localhost",
                Port = 389,
                Username = @"cn=admin,dc=example,dc=org",
                Password = "password",
                UseSsl = false,
                BaseDn = "dc=example,dc=org"
            };

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

        private void RemoveEntry(LdapEntry ldapEntry, LdapConnectionProvider ldapConnectionProvider)
        {
            using (var connection = ldapConnectionProvider.GetConnection())
            {
                connection.Delete(ldapEntry.DN);
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
    }
}
