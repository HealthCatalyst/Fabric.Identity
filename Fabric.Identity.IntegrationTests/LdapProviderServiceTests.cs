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
        public void FindUser_Succeeds()
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

            var ldapConnectionProvider = new LdapConnectionProvider(settings);
            var newUser = CreateTestUser("test", "user", settings.BaseDn, ldapConnectionProvider);
            var ldapProviderService = new LdapProviderService(ldapConnectionProvider, logger);
            var users = ldapProviderService.FindUserBySubjectId($"TEST\\{newUser.getAttribute("cn").StringValue}");
            Assert.NotNull(users);
            Assert.Equal("test", users.FirstName);
            Assert.Equal("user", users.LastName);
            Assert.Equal("test.user", users.Username);
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

        private LdapModification CreateLdapModification()
        {
            var attributeType = new LdapAttributeSchema(new[] {"objectCategory"}, "1.2.3.4.5.6", "category",
                "1.3.6.1.4.1.1466.115.121.1.15", true, null, false, null, null, null, false, true,
                LdapAttributeSchema.USER_APPLICATIONS);
            var objectClass = new LdapObjectClassSchema(new []{"user"}, "2.16.840.1133730.2.123", new []{"top"}, "user", null, null, LdapObjectClassSchema.STRUCTURAL, false);
            var modification = new LdapModification(LdapModification.ADD, attributeType);
            return modification;
        }
    }
}
