using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fabric.Identity.API.Exceptions;
using Fabric.Identity.API.Models;
using PrincipalType = Fabric.Identity.API.FabricIdentityEnums.PrincipalType;

namespace Fabric.Identity.API.Services
{
    public class PrincipalSearchService : IPrincipalSearchService
    {
        private readonly IEnumerable<IExternalIdentityProviderSearchService> _externalIdentityProviderServices;

        public PrincipalSearchService(IEnumerable<IExternalIdentityProviderSearchService> externalIdentityProviderServices)
        {
            _externalIdentityProviderServices = externalIdentityProviderServices;
        }

        public async Task<IEnumerable<FabricPrincipal>> SearchPrincipalsAsync(string searchText, string principalTypeString, string searchType,
            string identityProvider = null, string tenantId = null)
        {
            PrincipalType principalType;

            if (string.IsNullOrWhiteSpace(principalTypeString))
            {
                principalType = PrincipalType.UserAndGroup;
            }
            else if (principalTypeString.ToLowerInvariant().Equals("user"))
            {
                principalType = PrincipalType.User;
            }
            else if (principalTypeString.ToLowerInvariant().Equals("group"))
            {
                principalType = PrincipalType.Group;
            }
            else
            {
                throw new DirectorySearchException(
                    "invalid principal type provided. valid values are 'user' and 'group'");
            }

            var result = new List<FabricPrincipal>();
            var filteredServices =
                _externalIdentityProviderServices.Where(s =>
                    IsIdentityProviderMatch(identityProvider, s.IdentityProvider));
            foreach (var service in filteredServices)
            {
                result.AddRange(await service.SearchPrincipalsAsync(searchText, principalType, searchType, tenantId).ConfigureAwait(false));
            }

            return result;
        }

        public async Task<FabricPrincipal> FindUserBySubjectIdAsync(string subjectId, string tenantId)
        {
            foreach (var service in _externalIdentityProviderServices)
            {
                var subject = await service.FindUserBySubjectIdAsync(subjectId, tenantId);
                if (subject != null)
                {
                    return subject;
                }
            }

            return null;
        }

        public async Task<IEnumerable<FabricGroup>> SearchGroupsAsync(string searchText, string searchType,
            string identityProvider = null, string tenantId = null)
        {
            var groups = new List<FabricGroup>();
            var filteredServices =
                _externalIdentityProviderServices.Where(s =>
                    IsIdentityProviderMatch(identityProvider, s.IdentityProvider));

            foreach (var service in filteredServices)
            {
                groups.AddRange(await service.SearchGroupsAsync(searchText, searchType, tenantId)
                    .ConfigureAwait(false));
            }

            return groups;
        }

        private static bool IsIdentityProviderMatch(string incomingIdentityProvider, string serviceIdentityProvider)
        {
            return string.IsNullOrWhiteSpace(incomingIdentityProvider)
                   || string.Equals(incomingIdentityProvider, serviceIdentityProvider, StringComparison.OrdinalIgnoreCase);
        }
    }
}
