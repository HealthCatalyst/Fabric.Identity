using System;
using System.Collections.Generic;
using System.Linq;
using Fabric.Identity.API.Models;
using Fabric.Identity.API.Services;
using Moq;
using static Fabric.Identity.API.FabricIdentityEnums;

namespace Fabric.Identity.IntegrationTests
{
    public static class ActiveDirectoryProxyMockExtensions
    {
        private static string _userAndGroupSearchForPat =
            "(&(|(&(objectClass=user)(objectCategory=person))(objectCategory=group))(|(sAMAccountName=pat*)(givenName=pat*)(sn=pat*)(cn=pat*)(mail=pat*)))";

        private static string _groupSearchForPat =
            "(&(objectCategory=group)(|(sAMAccountName=pat*)(givenName=pat*)(sn=pat*)(cn=pat*)(mail=pat*)))";

        private static string _userSearchForPat =
            "(&(objectClass=user)(objectCategory=person)(|(sAMAccountName=pat*)(givenName=pat*)(sn=pat*)(cn=pat*)(mail=pat*)))";

        private static string _userSearchForPatrickJones =
            "(&(objectClass=user)(objectCategory=person)(|(sAMAccountName=patrick jones*)(givenName=patrick jones*)(sn=patrick jones*)(cn=patrick jones*)(mail=patrick jones*)))";

        private static readonly string _directorySearchForPat = "pat";
        private static readonly string _directorySearchForPatrickJones = "patrick jones";
        private static readonly string _identitySearchForPatrickJones = "patrick.jones";
        private static readonly string _identitySearchForPatrickJon = "patrick.jon";

        private static readonly Func<IDirectoryEntry, string, bool> DirectorySearchStartsWithPredicate =
            (de, searchText) =>
                de.FirstName.StartsWith(searchText, StringComparison.OrdinalIgnoreCase) ||
                de.SamAccountName.StartsWith(searchText, StringComparison.OrdinalIgnoreCase) ||
                de.LastName.StartsWith(searchText, StringComparison.OrdinalIgnoreCase) ||
                de.Name.StartsWith(searchText, StringComparison.OrdinalIgnoreCase) ||
                de.Email.StartsWith(searchText, StringComparison.OrdinalIgnoreCase);

        private static readonly Func<IDirectoryEntry, string, bool> IdentitySearchEqualsPredicate = (de, searchText) =>
            de.FirstName.Equals(searchText, StringComparison.OrdinalIgnoreCase) ||
            de.SamAccountName.Equals(searchText, StringComparison.OrdinalIgnoreCase) ||
            de.LastName.Equals(searchText, StringComparison.OrdinalIgnoreCase) ||
            de.Email.Equals(searchText, StringComparison.OrdinalIgnoreCase);

        public static Mock<IActiveDirectoryProxy> SetupActiveDirectoryProxy(this Mock<IActiveDirectoryProxy> mockAdProxy, IEnumerable<IDirectoryEntry> principals)
        {
            mockAdProxy.Setup(proxy => proxy.SearchDirectory(It.Is<string>(s => s == _userAndGroupSearchForPat)))
                .Returns((string ldapQuery) => principals.Where(p => DirectorySearchStartsWithPredicate(p, _directorySearchForPat)));

            mockAdProxy.Setup(proxy => proxy.SearchDirectory(It.Is<string>(s => s == _groupSearchForPat)))
                .Returns((string ldapQuery) =>
                {
                    return principals.Where(p => p.SchemaClassName.Equals("group") &&
                                                 DirectorySearchStartsWithPredicate(p, _directorySearchForPat));
                });

            mockAdProxy.Setup(proxy => proxy.SearchDirectory(It.Is<string>(s => s == _userSearchForPat)))
                .Returns((string ldapQuery) =>
                {
                    return principals.Where(p => p.SchemaClassName.Equals("user") &&
                                                 DirectorySearchStartsWithPredicate(p, _directorySearchForPat));
                });

            mockAdProxy.Setup(proxy => proxy.SearchDirectory(It.Is<string>(s => s == _userSearchForPatrickJones)))
                .Returns((string ldapQuery) =>
                {
                    return principals.Where(p => p.SchemaClassName.Equals("user") &&
                                                 DirectorySearchStartsWithPredicate(p, _directorySearchForPatrickJones));
                });

            mockAdProxy.Setup(proxy =>
                    proxy.SearchForUser(It.IsAny<string>(), It.Is<string>(s => s == _identitySearchForPatrickJones)))
                .Returns(() =>
                {
                    var userEntry =
                        principals.FirstOrDefault(p =>
                            IdentitySearchEqualsPredicate(p, _identitySearchForPatrickJones));

                    if (userEntry == null)
                    {
                        return null;
                    }

                    return new FabricPrincipal
                    {
                        FirstName = userEntry.FirstName,
                        LastName = userEntry.LastName,
                        MiddleName = userEntry.MiddleName,
                        PrincipalType = PrincipalType.User,
                        SubjectId =$"testing\\{userEntry.SamAccountName}",
                        Email = userEntry.Email
                    };
                });

            mockAdProxy.Setup(proxy =>
                    proxy.SearchForUser(It.IsAny<string>(), It.Is<string>(s => s == _identitySearchForPatrickJon)))
                .Returns(() =>
                {
                    var userEntry = principals.FirstOrDefault(p =>
                        IdentitySearchEqualsPredicate(p, _identitySearchForPatrickJon));

                    if (userEntry == null)
                    {
                        return null;
                    }

                    return new FabricPrincipal
                    {
                        FirstName = userEntry.FirstName,
                        LastName = userEntry.LastName,
                        MiddleName = userEntry.MiddleName,
                        PrincipalType = PrincipalType.User,
                        SubjectId = $"testing\\{userEntry.SamAccountName}",
                        Email = userEntry.Email
                    };
                });

            return mockAdProxy;
        }
    }
}
