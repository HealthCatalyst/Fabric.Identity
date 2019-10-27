using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using Fabric.Identity.API.Configuration;
using Fabric.Identity.API.Models;
using IdentityModel.Client;

namespace Fabric.Identity.API.Services.Azure
{
    public class AzureActiveDirectoryCacheService : IAzureActiveDirectoryClientCredentialsService
    {
        // String is tenant id and the Token response wrapper adds the expiry date time for easier cache invalidation.
        private static ConcurrentDictionary<string, TokenResponseWrapper> _tokensOfEachTenant;
        private static IDictionary<string, AzureClientApplicationSettings> _appSettings;
        private IAzureActiveDirectoryClientCredentialsService _innerCredentialService;

        static AzureActiveDirectoryCacheService()
        {
            _tokensOfEachTenant = new ConcurrentDictionary<string, TokenResponseWrapper>();
        }

        public AzureActiveDirectoryCacheService(IAppConfiguration appConfiguration, IAzureActiveDirectoryClientCredentialsService innerCredentialService)
        {
            _appSettings = AzureClientApplicationSettings.CreateDictionary(appConfiguration.AzureActiveDirectoryClientSettings);
            _innerCredentialService = innerCredentialService;

        }

        public async Task<TokenResponse> GetAzureAccessTokenAsync(string tenantId)
        {
            await GenerateAccessTokensAsync();
            TokenResponseWrapper token;
            if (_tokensOfEachTenant.TryGetValue(tenantId, out token))
            {
                return token.Response;
            }

            return null;
        }

        private async Task GenerateAccessTokensAsync()
        {
            var tenantIds = _appSettings.Keys;

            foreach (var tenantId in tenantIds)
            {
                TokenResponseWrapper token;

                if (_tokensOfEachTenant.TryGetValue(tenantId, out token))
                {
                    if (token.ExpiryTime <= DateTime.Now)
                    {
                        await GetNewTokenAsync(tenantId).ConfigureAwait(false);
                    }
                }
                else
                {
                    await GetNewTokenAsync(tenantId).ConfigureAwait(false);
                }

            }
        }

        private async Task GetNewTokenAsync(string tenantId)
        {
            var response = await _innerCredentialService.GetAzureAccessTokenAsync(tenantId).ConfigureAwait(false);
            var newToken = new TokenResponseWrapper() { ExpiryTime = DateTime.Now.AddSeconds(response.ExpiresIn), Response = response };
            _tokensOfEachTenant.AddOrUpdate(tenantId, newToken, (key, oldValue) => { return newToken; });
        }
    }
}
