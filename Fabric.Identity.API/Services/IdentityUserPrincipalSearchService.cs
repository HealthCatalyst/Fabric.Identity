using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Fabric.Identity.API.Models;
using Fabric.Identity.API.Persistence;

namespace Fabric.Identity.API.Services
{
    public class IdentityUserPrincipalSearchService : IExternalIdentityProviderSearchService
    {
        private readonly IMapper _mapper;
        private readonly IUserStore _userStore;

        public string IdentityProvider => FabricIdentityConstants.SearchIdentityProviders.IdentityDatabase;

        public IdentityUserPrincipalSearchService(IMapper mapper, IUserStore userStore)
        {
            _mapper = mapper;
            _userStore = userStore;
        }

        public async Task<IEnumerable<FabricPrincipal>> SearchPrincipalsAsync(string searchText, FabricIdentityEnums.PrincipalType principalType, string searchType, string tenantId = null)
        {
            if (principalType == FabricIdentityEnums.PrincipalType.Group)
            {
                return new List<FabricPrincipal>();
            }

            var users = await _userStore.SearchUsersAsync(searchText, searchType);

            return _mapper.Map<IEnumerable<FabricPrincipal>>(users);
        }

        public Task<IEnumerable<FabricGroup>> SearchGroupsAsync(string searchText, string searchType, string tenantId = null)
        {
            return Task.FromResult(new List<FabricGroup>().AsEnumerable());
        }

        public async Task<FabricPrincipal> FindUserBySubjectIdAsync(string subjectId, string tenantId = null)
        {
            var user = await _userStore.FindBySubjectIdAsync(subjectId);
            return _mapper.Map<FabricPrincipal>(user);
        }
    }
}
