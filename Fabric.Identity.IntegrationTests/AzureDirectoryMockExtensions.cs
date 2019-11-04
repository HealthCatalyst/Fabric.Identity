using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fabric.Identity.API.Models;
using Fabric.Identity.API.Services.Azure;
using Moq;

namespace Fabric.Identity.IntegrationTests
{

    public static class AzureDirectoryMockExtensions
    {
        private static string getITGroupWildCard =
        "startswith(DisplayName, 'ITGroup')";

        private static string getITExact =
        "DisplayName eq 'IT'";

        private static readonly string directorySearchForIT = "IT";

        private static readonly string directorySearchForITGroup = "ITGroup";

        private static string getUserWildCard =
            "startswith(DisplayName, 'johnny') or startswith(UserPrincipalName, 'johnny') or startswith(Surname, 'johnny') or startswith(GivenName, 'johnny') or startswith(Mail, 'johnny')";

        private static readonly string directorySearchForUserJohnny = "johnny";

        private static readonly string directorySearchForJamesRocket = "testingAzure\\james rocket";

        private static readonly Func<FabricGraphApiGroup, string, bool> AzureGroupSearchStartsWithPredicate =
            (fg, searchText) =>
                fg.Group.DisplayName.StartsWith(searchText, StringComparison.OrdinalIgnoreCase);

        private static readonly Func<FabricGraphApiGroup, string, bool> AzureGroupSearchEqualsPredicate =
            (fg, searchText) =>
                fg.Group.DisplayName.Equals(searchText, StringComparison.OrdinalIgnoreCase);

        private static readonly Func<FabricGraphApiUser, string, string, bool> AzureUserSearchStartsWithPredicate =
            (fg, searchText, tenant) =>
                fg.TenantId.Equals(tenant, StringComparison.OrdinalIgnoreCase) && fg.User.DisplayName.StartsWith(searchText, StringComparison.OrdinalIgnoreCase) ||
                fg.User.GivenName.StartsWith(searchText, StringComparison.OrdinalIgnoreCase) ||
                fg.User.UserPrincipalName.StartsWith(searchText, StringComparison.OrdinalIgnoreCase) ||
                fg.User.Surname.StartsWith(searchText, StringComparison.OrdinalIgnoreCase) ||
                (fg.User.Mail != null && fg.User.Mail.StartsWith(searchText, StringComparison.OrdinalIgnoreCase));

        public static Mock<IMicrosoftGraphApi> SetupAzureDirectoryGraphGroups(this Mock<IMicrosoftGraphApi> mockAdGraphGroups, IEnumerable<FabricGraphApiGroup> principals)
        {
            mockAdGraphGroups.Setup(p => p.GetGroupCollectionsAsync(getITGroupWildCard, null))
            .Returns((string filterQuery, string tenantId) =>
            {
                return Task.FromResult(principals.Where(g => AzureGroupSearchStartsWithPredicate(g, directorySearchForITGroup)));
            });

            mockAdGraphGroups.Setup(p => p.GetGroupCollectionsAsync(getITGroupWildCard, "1"))
            .Returns((string filterQuery, string tenantId) =>
            {
                return Task.FromResult(principals.Where(g => AzureGroupSearchStartsWithPredicate(g, directorySearchForITGroup)));
            });
            
            mockAdGraphGroups.Setup(p => p.GetGroupCollectionsAsync(getITExact, null))
            .Returns(() =>
            {
                var groupEntry =
                    principals.FirstOrDefault(p =>
                        AzureGroupSearchEqualsPredicate(p, directorySearchForIT));

                if (groupEntry == null)
                {
                    return null;
                }

                List<FabricGraphApiGroup> group = new List<FabricGraphApiGroup>();
                group.Add(groupEntry);
                return Task.FromResult((IEnumerable<FabricGraphApiGroup>)group);
            });

            mockAdGraphGroups.Setup(p => p.GetGroupCollectionsAsync(getITExact, "2"))
            .Returns(() =>
            {
                var groupEntry =
                    principals.FirstOrDefault(p =>
                        AzureGroupSearchEqualsPredicate(p, directorySearchForIT));

                if (groupEntry == null)
                {
                    return null;
                }

                List<FabricGraphApiGroup> group = new List<FabricGraphApiGroup>();
                group.Add(groupEntry);
                return Task.FromResult((IEnumerable<FabricGraphApiGroup>)group);
            });

            return mockAdGraphGroups;
        }

        public static Mock<IMicrosoftGraphApi> SetupAzureDirectoryGraphUsers(this Mock<IMicrosoftGraphApi> mockAdGraphUsers, IEnumerable<FabricGraphApiUser> principals)
        {
            mockAdGraphUsers.Setup(p => p.GetUserCollectionsAsync(getUserWildCard, null))
            .Returns((string filterQuery, string tenantId) =>
            {
                return Task.FromResult(principals.Where(g => AzureUserSearchStartsWithPredicate(g, directorySearchForUserJohnny, tenantId)));
            });

            mockAdGraphUsers.Setup(p => p.GetUserCollectionsAsync(getUserWildCard, "1"))
            .Returns((string filterQuery, string tenantId) =>
            {
                return Task.FromResult(principals.Where(g => g.TenantId.Equals(tenantId, StringComparison.OrdinalIgnoreCase) && 
                                                             AzureUserSearchStartsWithPredicate(g, directorySearchForUserJohnny, tenantId)));
            });

            mockAdGraphUsers.Setup(p => p.GetUserCollectionsAsync(getUserWildCard, "2"))
            .Returns((string filterQuery, string tenantId) =>
            {
                return Task.FromResult(principals.Where(g => g.TenantId.Equals(tenantId, StringComparison.OrdinalIgnoreCase) &&
                                                             AzureUserSearchStartsWithPredicate(g, directorySearchForUserJohnny, tenantId)));
            });

            return mockAdGraphUsers;
        }

        public static Mock<IMicrosoftGraphApi> SetupAzureDirectoryGraphUser(this Mock<IMicrosoftGraphApi> mockAdGraphUser, FabricGraphApiUser principal)
        {
            mockAdGraphUser.Setup(p => p.GetUserAsync(directorySearchForJamesRocket, null))
            .Returns((string subjectId, string tenantId) => Task.FromResult(principal));
            

            mockAdGraphUser.Setup(p => p.GetUserAsync(directorySearchForJamesRocket, "1"))
            .Returns((string subjectId, string tenantId) => Task.FromResult(principal));
            return mockAdGraphUser;
        }
    }
}
