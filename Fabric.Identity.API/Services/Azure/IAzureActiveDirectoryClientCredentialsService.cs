using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using IdentityModel.Client;

namespace Fabric.Identity.API.Services.Azure
{
    public interface IAzureActiveDirectoryClientCredentialsService
    {
        Task<TokenResponse> GetAzureAccessTokenAsync(string tenantId);
    }
}
