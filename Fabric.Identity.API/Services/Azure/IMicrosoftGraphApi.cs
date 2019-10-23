using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fabric.Identity.API.Models;

namespace Fabric.Identity.API.Services.Azure
{
    public interface IMicrosoftGraphApi
    {
        Task<IEnumerable<FabricGraphApiGroup>> GetGroupCollectionsAsync(string filterQuery, string tenantId = null);
        Task<FabricGraphApiUser> GetUserAsync(string subjectId, string tenantId = null);
        Task<IEnumerable<FabricGraphApiUser>> GetUserCollectionsAsync(string filterQuery, string tenantId = null);
    }
}
