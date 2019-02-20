using System.Threading.Tasks;
using Fabric.Identity.API.Extensions;
using Fabric.Identity.API.Services;

namespace Fabric.Identity.API.Configuration
{
    public class IdentityProviderSearchSettings
    {
        /// <summary>
        /// NOTE: Do not use this for retrieving the IdPSS URL. Instead, use <code>GetEffectiveBaseUrl</code>.
        /// </summary>
        public string BaseUrl { get; set; }
        public string GetUserEndpoint { get; set; }
        public bool IsEnabled { get; set; }

        private string _effectiveBaseUrl;

        public async Task<string> GetEffectiveBaseUrl(IAppConfiguration appConfig)
        {
            if (string.IsNullOrEmpty(_effectiveBaseUrl))
            {
                _effectiveBaseUrl = await GetBaseUrl(appConfig);
            }
            return _effectiveBaseUrl;
        }

        private static async Task<string> GetBaseUrl(IAppConfiguration appConfig)
        {
            if (!appConfig.UseDiscoveryService)
            {
                return appConfig.IdentityProviderSearchSettings.BaseUrl.EnsureTrailingSlash();
            }

            using (var discoveryServiceClient = new DiscoveryServiceClient(appConfig.DiscoveryServiceEndpoint))
            {
                var serviceRegistration = await discoveryServiceClient.GetServiceAsync("IdentityProviderSearchService", 1);

                return !string.IsNullOrEmpty(serviceRegistration?.ServiceUrl) ? serviceRegistration.ServiceUrl.EnsureTrailingSlash() : appConfig.IdentityProviderSearchSettings.BaseUrl.EnsureTrailingSlash();
            }
        }
    }
}