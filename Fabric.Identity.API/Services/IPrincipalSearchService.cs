using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fabric.Identity.API.Models;

namespace Fabric.Identity.API.Services
{
    public interface IPrincipalSearchService
    {
        Task<IEnumerable<FabricPrincipal>> SearchPrincipalsAsync(string searchText, string principalTypeString,
            string searchType, string identityProvider = null, string tenantId = null);

        Task<FabricPrincipal> FindUserBySubjectIdAsync(string subjectId, string tenantId);

        Task<IEnumerable<FabricGroup>> SearchGroupsAsync(string searchText, string searchType,
            string identityProvider = null, string tenantId = null);
    }
}
