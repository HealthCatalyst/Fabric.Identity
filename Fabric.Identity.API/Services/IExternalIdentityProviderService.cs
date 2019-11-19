using System.Collections.Generic;
using System.Threading.Tasks;
using Fabric.Identity.API.Models;

namespace Fabric.Identity.API.Services
{
    public interface IExternalIdentityProviderService
    {
        Task<FabricPrincipal> FindUserBySubjectIdAsync(string subjectId);
        ICollection<FabricPrincipal> SearchUsers(string searchText);
    }
}
