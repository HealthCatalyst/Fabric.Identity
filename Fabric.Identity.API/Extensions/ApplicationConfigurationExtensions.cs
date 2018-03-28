using System.Threading.Tasks;


using Fabric.Identity.API.Configuration;
using Fabric.Identity.API.Services;

namespace Fabric.Identity.API.Extensions
{

    public static class ApplicationConfigurationExtensions
    {
        /// <summary>
        /// Configures the IdentitySearchProviderService Url in IAppConfiguration.
        /// </summary>
        /// <param name="appConfig">The <see cref="IAppConfiguration"/> instance to configure.</param>
        public static void ConfigureIdentitySearchProviderServiceUrl(this IAppConfiguration appConfig)
        {
            if (!appConfig.UseDiscoveryService)
            {
                return;
            }

            using (var discoveryServiceClient = new DiscoveryServiceClient(appConfig.DiscoveryServiceEndpoint))
            {
                var serviceRegistration = Task
                    .Run(() => discoveryServiceClient.GetServiceAsync("IdentityProviderSearchService", 1))
                    .Result;

                if (!string.IsNullOrEmpty(serviceRegistration?.ServiceUrl))
                {
                    appConfig.IdentityProviderSearchSettings.BaseUrl =
                        serviceRegistration.ServiceUrl;
                }
            }
        }
    }
}
