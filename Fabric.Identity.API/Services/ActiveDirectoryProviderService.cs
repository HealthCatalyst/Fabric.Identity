using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fabric.Identity.API.Configuration;
using Fabric.Identity.API.Models;
using Fabric.Identity.API.Services.PrincipalQuery;
using Microsoft.Security.Application;

namespace Fabric.Identity.API.Services
{
    public class ActiveDirectoryProviderService : IExternalIdentityProviderSearchService
    {
        private readonly IActiveDirectoryProxy _activeDirectoryProxy;
        private readonly string _domain;
        private IActiveDirectoryQuery _activeDirectoryQuery;

        public string IdentityProvider => FabricIdentityConstants.SearchIdentityProviders.ActiveDirectory;

        public ActiveDirectoryProviderService(IActiveDirectoryProxy activeDirectoryProxy, IAppConfiguration appConfig)
        {
            _activeDirectoryProxy = activeDirectoryProxy;
            _domain = appConfig.DomainName;
        }

        public async Task<IEnumerable<FabricPrincipal>> SearchPrincipalsAsync(
            string searchText,
            FabricIdentityEnums.PrincipalType principalType,
            string searchType,
            string tenantId = null)
        {
            switch (searchType)
            {
                case FabricIdentityConstants.SearchTypes.Wildcard:
                    _activeDirectoryQuery = new ActiveDirectoryWildcardQuery();
                    break;
                case FabricIdentityConstants.SearchTypes.Exact:
                    _activeDirectoryQuery = new ActiveDirectoryExactMatchQuery();
                    break;
                default:
                    throw new Exception($"{searchType} is not a valid search type");
            }
            var ldapQuery = _activeDirectoryQuery.QueryText(searchText, principalType);
            var principals = await Task.Run(() => FindPrincipalsWithDirectorySearcher(ldapQuery)).ConfigureAwait(false);
            return principals;
        }

        public async Task<FabricPrincipal> FindUserBySubjectIdAsync(string subjectId, string tenantId = null)
        {
            if (!subjectId.Contains(@"\"))
            {
                return null;
            }

            var subjectIdParts = subjectId.Split('\\');
            var domain = subjectIdParts[0];
            var accountName = subjectIdParts[subjectIdParts.Length - 1];

            var subject = await Task.Run(() => _activeDirectoryProxy.SearchForUser(Encoder.LdapFilterEncode(domain), Encoder.LdapFilterEncode(accountName))).ConfigureAwait(false);

            if (subject == null)
            {
                return null;
            }

            var principal = CreateUserPrincipal(subject);
            return principal;
        }

        public Task<IEnumerable<FabricGroup>> SearchGroupsAsync(
            string searchText,
            string searchType,
            string tenantId = null)
        {
            var fabricGroups = new List<FabricGroup>();

            switch (searchType)
            {
                case FabricIdentityConstants.SearchTypes.Wildcard:
                    _activeDirectoryQuery = new ActiveDirectoryWildcardQuery();
                    break;
                case FabricIdentityConstants.SearchTypes.Exact:
                    _activeDirectoryQuery = new ActiveDirectoryExactMatchQuery();
                    break;
                default:
                    throw new Exception($"{searchType} is not a valid search type");
            }

            var ldapQuery = _activeDirectoryQuery.QueryText(searchText, FabricIdentityEnums.PrincipalType.Group);
            var searchResults = _activeDirectoryProxy.SearchDirectory(ldapQuery);

            foreach (var searchResult in searchResults)
            {
                if (!IsDirectoryEntryAUser(searchResult))
                {
                    fabricGroups.Add(CreateFabricGroup(searchResult));
                }
            }
            return Task.FromResult(fabricGroups.AsEnumerable());
        }

        private IEnumerable<FabricPrincipal> FindPrincipalsWithDirectorySearcher(string ldapQuery)
        {
            var principals = new List<FabricPrincipal>();

            var searchResults = _activeDirectoryProxy.SearchDirectory(ldapQuery);

            foreach (var searchResult in searchResults)
            {
                principals.Add(IsDirectoryEntryAUser(searchResult)
                    ? CreateUserPrincipal(searchResult)
                    : CreateGroupPrincipal(searchResult));
            }
            return principals;
        }

        private static bool IsDirectoryEntryAUser(IDirectoryEntry entryResult)
        {
            // TODO: Add to constants file
            return entryResult.SchemaClassName.Equals("user");
        }

        private FabricPrincipal CreateUserPrincipal(IDirectoryEntry userEntry)
        {
            var subjectId = GetSubjectId(userEntry.SamAccountName);
            var principal = new FabricPrincipal
            {
                SubjectId = subjectId,
                DisplayName = $"{userEntry.FirstName} {userEntry.LastName}",
                FirstName = userEntry.FirstName,
                LastName = userEntry.LastName,
                MiddleName = userEntry.MiddleName,
                IdentityProvider = FabricIdentityConstants.SearchIdentityProviders.ActiveDirectory,
                PrincipalType = FabricIdentityEnums.PrincipalType.User,
                IdentityProviderUserPrincipalName = subjectId,
                Email = userEntry.Email
            };

            principal.DisplayName = $"{principal.FirstName} {principal.LastName}";
            return principal;
        }
        private static FabricPrincipal CreateUserPrincipal(FabricPrincipal userEntry)
        {
            var principal = new FabricPrincipal
            {
                UserPrincipal = userEntry.UserPrincipal,
                TenantId = userEntry.TenantId,
                FirstName = userEntry.FirstName,
                LastName = userEntry.LastName,
                MiddleName = userEntry.MiddleName,
                IdentityProvider = FabricIdentityConstants.SearchIdentityProviders.ActiveDirectory,
                PrincipalType = FabricIdentityEnums.PrincipalType.User,
                SubjectId = userEntry.SubjectId,
                IdentityProviderUserPrincipalName = userEntry.SubjectId,
                Email = userEntry.Email
            };

            principal.DisplayName = $"{principal.FirstName} {principal.LastName}";
            return principal;
        }

        private FabricPrincipal CreateGroupPrincipal(IDirectoryEntry groupEntry)
        {
            var subjectId = GetSubjectId(groupEntry.Name);
            return new FabricPrincipal
            {
                SubjectId = subjectId,
                DisplayName = subjectId,
                IdentityProvider = FabricIdentityConstants.SearchIdentityProviders.ActiveDirectory,
                PrincipalType = FabricIdentityEnums.PrincipalType.Group
            };
        }

        private FabricGroup CreateFabricGroup(IDirectoryEntry groupEntry)
        {
            var subjectId = GetSubjectId(groupEntry.Name);
            return new FabricGroup
            {
                GroupName = subjectId,
                IdentityProvider = FabricIdentityConstants.SearchIdentityProviders.ActiveDirectory,
                PrincipalType = FabricIdentityEnums.PrincipalType.Group
            };
        }

        private string GetSubjectId(string sAmAccountName)
        {
            return $"{_domain}\\{sAmAccountName}";
        }

        public ICollection<FabricPrincipal> SearchUsers(string searchText)
        {
            throw new NotImplementedException();
        }
    }
}
