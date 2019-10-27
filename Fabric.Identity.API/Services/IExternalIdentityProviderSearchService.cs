using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fabric.Identity.API.Models;

namespace Fabric.Identity.API.Services
{
    public interface IExternalIdentityProviderSearchService
    {
        string IdentityProvider { get; }
        Task<IEnumerable<FabricPrincipal>> SearchPrincipalsAsync(string searchText,
            FabricIdentityEnums.PrincipalType principalType, string searchType, string tenantId = null);

        Task<IEnumerable<FabricGroup>> SearchGroupsAsync(string searchText, string searchType, string tenantId = null);
        Task<FabricPrincipal> FindUserBySubjectIdAsync(string subjectId, string tenantId = null);
    }
}
