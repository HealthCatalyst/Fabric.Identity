using Fabric.Identity.API;
using Fabric.Identity.API.Exceptions;
using IdentityModel.Client;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Fabric.Identity.API.Configuration;
using Microsoft.Extensions.Localization;

namespace Fabric.Identity.API.Services.Azure
{
    public class AzureActiveDirectoryClientCredentialsService : IAzureActiveDirectoryClientCredentialsService
    {
        private HttpClient _client;
        private string _authority;
        private string _tokenEndpoint;
        private IDictionary<string, AzureClientApplicationSettings> _settings;
        private IStringLocalizer<AzureActiveDirectoryClientCredentialsService> _localizer;

        public AzureActiveDirectoryClientCredentialsService(IAppConfiguration appSettings,
            HttpClient client, IStringLocalizer<AzureActiveDirectoryClientCredentialsService> localizer)
        {
            var azureClientSettings = appSettings.AzureActiveDirectoryClientSettings;
            this._settings = AzureClientApplicationSettings.CreateDictionary(azureClientSettings);
            this._authority = azureClientSettings.Authority;
            this._tokenEndpoint = azureClientSettings.TokenEndpoint;
            this._client = client;
            _localizer = localizer;
        }

        public async Task<TokenResponse> GetAzureAccessTokenAsync(string tenantId)
        {
            var appSettings = this._settings[tenantId];
            var client = new HttpClient();
            var tokenRequest = new ClientCredentialsTokenRequest
            {
                Address = $"{this._authority.TrimEnd('/')}/{tenantId}/{_tokenEndpoint}",
                ClientId = appSettings.ClientId,
                ClientSecret = appSettings.ClientSecret,
                ClientCredentialStyle = ClientCredentialStyle.PostBody,
                Scope = appSettings.Scopes.First()
            };

            var response = await client.RequestClientCredentialsTokenAsync(tokenRequest);

            if (!response.IsError)
            {
                return response;
            }

            string exceptionMessage = _localizer["AzureAccessTokenRetrievalFailure"];
            throw new AzureActiveDirectoryException(exceptionMessage);
        }
    }
}
