using Fabric.Identity.API.Models;
using System;
using System.Collections.Generic;
using System.DirectoryServices;
using System.DirectoryServices.AccountManagement;
using System.Linq;
using System.Threading.Tasks;

namespace Fabric.Identity.API.Services
{
    public class ActiveDirectoryProxy : IActiveDirectoryProxy
    {
        public IEnumerable<IDirectoryEntry> SearchDirectory(string ldapQuery)
        {
            using (var directorySearcher = new DirectorySearcher(null, ldapQuery))
            {
                var results = directorySearcher.FindAll();

                var searchResults = new List<IDirectoryEntry>();

                foreach (SearchResult searchResult in results)
                {
                    searchResults.Add(new DirectoryEntryWrapper(searchResult.GetDirectoryEntry()));
                }

                return searchResults;
            }
        }

        public FabricPrincipal SearchForUser(string domain, string accountName)
        {
            using (var ctx = new PrincipalContext(ContextType.Domain, domain))
            {
                var userPrincipalResult = UserPrincipal.FindByIdentity(ctx, IdentityType.SamAccountName, accountName);

                if (userPrincipalResult == null)
                {
                    return null;
                }

                return new FabricPrincipal
                {
                    FirstName = userPrincipalResult.GivenName,
                    MiddleName = userPrincipalResult.MiddleName,
                    LastName = userPrincipalResult.Surname,
                    Email = userPrincipalResult.EmailAddress,
                    SubjectId = GetSubjectId(domain, userPrincipalResult.SamAccountName),
                    PrincipalType = FabricIdentityEnums.PrincipalType.User
                };
            }
        }

        private string GetSubjectId(string domain, string sAmAccountName)
        {
            return $"{domain}\\{sAmAccountName}";
        }
    }
}
