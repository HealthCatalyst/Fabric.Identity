using System.Collections.Generic;
using Fabric.Identity.API.Models;

namespace Fabric.Identity.API.Services
{
    public interface IExternalIdentityProviderService
    {
        ExternalUser FindUserBySubjectId(string subjectId);
        ICollection<ExternalUser> SearchUsers(string searchText);
    }
}
