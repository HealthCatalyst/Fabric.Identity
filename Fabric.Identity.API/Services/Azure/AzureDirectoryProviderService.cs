using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fabric.Identity.API.Exceptions;
using Fabric.Identity.API.Models;
using Fabric.Identity.API.Services.PrincipalQuery;
using Serilog;
using Microsoft.Graph;

namespace Fabric.Identity.API.Services.Azure
{
    public class AzureDirectoryProviderService : IExternalIdentityProviderSearchService
    {
        private readonly IMicrosoftGraphApi _graphApi;
        private IAzureQuery _azureQuery;
        private ILogger _logger;

        public string IdentityProvider => FabricIdentityConstants.SearchIdentityProviders.AzureActiveDirectory;

        public AzureDirectoryProviderService(IMicrosoftGraphApi graphApi, ILogger logger)
        {
            _graphApi = graphApi;
            _logger = logger;
        }

        public async Task<FabricPrincipal> FindUserBySubjectIdAsync(string subjectId, string tenantId = null)
        {
            try
            {
                var result = await _graphApi.GetUserAsync(subjectId, tenantId).ConfigureAwait(false);
                if (result == null)
                {
                    return null;
                }

                var principal = CreateUserPrincipal(result);
                return principal;
            }
            catch (ServiceException e)
            {
                _logger.Information($"Exception thrown while searching for user {subjectId} in Azure AD: {e}");
                return null;
            }
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
                    _azureQuery = new AzureWildcardQuery();
                    break;
                case FabricIdentityConstants.SearchTypes.Exact:
                    _azureQuery = new AzureExactMatchQuery();
                    break;
                default:
                    throw new DirectorySearchException($"{searchType} is not a valid search type");
            }

            switch (principalType)
            {
                case FabricIdentityEnums.PrincipalType.User:
                    return await GetUserPrincipalsAsync(searchText, tenantId).ConfigureAwait(false);
                case FabricIdentityEnums.PrincipalType.Group:
                    return await GetGroupPrincipalsAsync(searchText, tenantId).ConfigureAwait(false);
                default:
                    return await GetUserAndGroupPrincipalsAsync(searchText, tenantId).ConfigureAwait(false);
            }
        }

        public async Task<IEnumerable<FabricGroup>> SearchGroupsAsync(string searchText, string searchType, string tenantId = null)
        {
            var fabricGroups = new List<FabricGroup>();

            switch (searchType)
            {
                case FabricIdentityConstants.SearchTypes.Wildcard:
                    _azureQuery = new AzureWildcardQuery();
                    break;
                case FabricIdentityConstants.SearchTypes.Exact:
                    _azureQuery = new AzureExactMatchQuery();
                    break;
                default:
                    throw new DirectorySearchException($"{searchType} is not a valid search type");
            }

            var queryText = _azureQuery.QueryText(searchText, FabricIdentityEnums.PrincipalType.Group);
            var fabricGraphApiGroups = await GetAllGroupsFromTenantsAsync(queryText, tenantId);
            foreach (var fabricGraphApiGroup in fabricGraphApiGroups)
            {
                fabricGroups.Add(CreateFabricGroup(fabricGraphApiGroup));
            }
            return fabricGroups;
        }

        private async Task<IEnumerable<FabricPrincipal>> GetUserAndGroupPrincipalsAsync(string searchText, string tenantId = null)
        {
            var userSearchTask = GetUserPrincipalsAsync(searchText, tenantId);
            var groupSearchTask = GetGroupPrincipalsAsync(searchText, tenantId);
            var results = await Task.WhenAll(userSearchTask, groupSearchTask).ConfigureAwait(false);
            return results.SelectMany(result => result);
        }

        private async Task<IEnumerable<FabricPrincipal>> GetUserPrincipalsAsync(string searchText, string tenantId = null)
        {
            string queryText = null;
            var principals = new List<FabricPrincipal>();
            queryText = _azureQuery.QueryText(searchText, FabricIdentityEnums.PrincipalType.User);
            var users = await GetAllUsersFromTenantsAsync(queryText, tenantId).ConfigureAwait(false);

            foreach (var result in users)
            {
                principals.Add(CreateUserPrincipal(result));
            }

            return principals;
        }

        private async Task<IEnumerable<FabricGraphApiUser>> GetAllUsersFromTenantsAsync(string searchText, string tenantId = null)
        {
            return await _graphApi.GetUserCollectionsAsync(searchText, tenantId).ConfigureAwait(false);
        }

        private async Task<IEnumerable<FabricPrincipal>> GetGroupPrincipalsAsync(string searchText, string tenantId = null)
        {
            string queryText = null;
            var principals = new List<FabricPrincipal>();
            queryText = _azureQuery.QueryText(searchText, FabricIdentityEnums.PrincipalType.Group);
            var groups = await GetAllGroupsFromTenantsAsync(queryText, tenantId).ConfigureAwait(false);

            if (groups != null)
            {
                foreach (var result in groups)
                {
                    principals.Add(CreateGroupPrincipal(result));
                }

                return principals;
            }
            return null;
        }

        private async Task<IEnumerable<FabricGraphApiGroup>> GetAllGroupsFromTenantsAsync(string searchText, string tenantId = null)
        {
            return await _graphApi.GetGroupCollectionsAsync(searchText, tenantId).ConfigureAwait(false);
        }

        private static FabricPrincipal CreateUserPrincipal(FabricGraphApiUser userEntry)
        {
            var principal = new FabricPrincipal
            {
                UserPrincipal = userEntry.User.UserPrincipalName,
                TenantId = userEntry.TenantId,
                TenantAlias = userEntry.TenantAlias ?? userEntry.TenantId,
                FirstName = userEntry.User.GivenName ?? userEntry.User.DisplayName,
                LastName = userEntry.User.Surname,
                MiddleName = string.Empty,   // this value does not exist in the graph api
                IdentityProvider = FabricIdentityConstants.SearchIdentityProviders.AzureActiveDirectory,
                PrincipalType = FabricIdentityEnums.PrincipalType.User,
                SubjectId = userEntry.User.Id,
                IdentityProviderUserPrincipalName = string.IsNullOrEmpty(userEntry.User.Mail)
                    ? userEntry.User.UserPrincipalName
                    : userEntry.User.Mail
            };

            principal.Email = principal.IdentityProviderUserPrincipalName;
            principal.DisplayName = $"{principal.FirstName} {principal.LastName}";
            return principal;
        }

        private static FabricPrincipal CreateGroupPrincipal(FabricGraphApiGroup groupEntry)
        {
            var result = new FabricPrincipal
            {
                SubjectId = groupEntry.Group.DisplayName,
                ExternalIdentifier = groupEntry.Group.Id,
                TenantId = groupEntry.TenantId,
                TenantAlias = groupEntry.TenantAlias ?? groupEntry.TenantId,
                DisplayName = groupEntry.Group.DisplayName,
                IdentityProvider = FabricIdentityConstants.SearchIdentityProviders.AzureActiveDirectory,
                PrincipalType = FabricIdentityEnums.PrincipalType.Group
            };

            return result;
        }

        private static FabricGroup CreateFabricGroup(FabricGraphApiGroup groupEntry)
        {
            return new FabricGroup
            {
                ExternalIdentifier = groupEntry.Group.Id,
                TenantId = groupEntry.TenantId,
                TenantAlias = groupEntry.TenantAlias ?? groupEntry.TenantId,
                GroupName = groupEntry.Group.DisplayName,
                IdentityProvider = FabricIdentityConstants.SearchIdentityProviders.AzureActiveDirectory,
                PrincipalType = FabricIdentityEnums.PrincipalType.Group
            };
        }

        public ICollection<FabricPrincipal> SearchUsers(string searchText)
        {
            throw new NotImplementedException();
        }
    }
}
