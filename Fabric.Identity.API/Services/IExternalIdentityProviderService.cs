using System.Collections.Generic;
using System.Threading.Tasks;
using Fabric.Identity.API.Models;

namespace Fabric.Identity.API.Services
{
    public interface IExternalIdentityProviderService
    {
        Task<ExternalUser> FindUserBySubjectId(string subjectId);
        ICollection<ExternalUser> SearchUsers(string searchText);
    }
}
