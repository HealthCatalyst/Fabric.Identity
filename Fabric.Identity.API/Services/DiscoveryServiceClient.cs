﻿using System;
using System.Threading.Tasks;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Linq;

using Fabric.Identity.API.Extensions;
using Fabric.Identity.API.Models;

using Newtonsoft.Json;


namespace Fabric.Identity.API.Services
{
    /// <summary>
    /// Client for interacting with the DiscoveryService.
    /// </summary>
    public class DiscoveryServiceClient : IDisposable
    {
        /// <summary>
        /// The HttpClient for making HTTP calls.
        /// </summary>
        private readonly HttpClient httpClient;

        /// <summary>
        /// Initializes a new instance of the <see cref="DiscoveryServiceClient"/> class.
        /// </summary>
        /// <param name="discoveryServiceUrl">The URL (including version) of the DiscoveryService.</param>
        public DiscoveryServiceClient(string discoveryServiceUrl) : this(discoveryServiceUrl, new HttpClientHandler { UseDefaultCredentials = true })
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DiscoveryServiceClient"/> class.
        /// </summary>
        /// <param name="discoveryServiceUrl">The URL (including version) of the DiscoveryService.</param>
        /// <param name="handler">The optional message handler for processing requests.</param>
        public DiscoveryServiceClient(string discoveryServiceUrl, HttpMessageHandler handler)
        {
            this.httpClient = new HttpClient(handler) { BaseAddress = new Uri(discoveryServiceUrl.EnsureTrailingSlash()) };
            this.httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        }

        /// <summary>
        /// Registers a service with DiscoveryService.
        /// </summary>
        /// <param name="discoveryServiceApi">The <see cref="DiscoveryServiceApiModel"/> to register.</param>
        /// <returns>A boolean value indicating success.</returns>
        public Task<bool> RegisterServiceAsync(DiscoveryServiceApiModel discoveryServiceApi)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Gets the registration for a service from DiscoveryService.
        /// </summary>
        /// <param name="serviceName">The name of the service to retrieve.</param>
        /// <param name="serviceVersion">The version of the service to retrieve.</param>
        /// <returns>A <see cref="DiscoveryServiceApiModel"/></returns>
        public async Task<DiscoveryServiceApiModel> GetServiceAsync(string serviceName, int serviceVersion)
        {
            var url = $"Services?$filter=ServiceName eq '{serviceName}' and Version eq {serviceVersion}";
            var response = await this.httpClient.GetAsync(url).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
            try
            {
                var apiModel = JsonConvert.DeserializeObject<DiscoveryServiceResponseModel>(
                    await response.Content.ReadAsStringAsync().ConfigureAwait(false));

                if (apiModel.Value.Count > 1)
                {
                    throw new InvalidOperationException($"Retrieved multiple {serviceName} version {serviceVersion} from DiscoveryService at {response.RequestMessage.RequestUri}");
                }

                var serviceRegistration = apiModel.Value.SingleOrDefault();
                if (serviceRegistration == null)
                {
                    throw new InvalidOperationException($"Could not get {serviceName} version {serviceVersion} from DiscoveryService at {response.RequestMessage.RequestUri}");
                }

                return serviceRegistration;
            }
            catch (JsonException e)
            {
                throw new InvalidOperationException(
                    $"Could not get {serviceName} version {serviceVersion} from DiscoveryService at {response.RequestMessage.RequestUri}",
                    e);
            }
        }

        /// <summary>
        /// Cleans up resources.
        /// </summary>
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases managed resources. 
        /// </summary>
        /// <param name="disposing">Flag to indicate whether to release managed resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                this.httpClient?.Dispose();
            }
        }
    }
}
