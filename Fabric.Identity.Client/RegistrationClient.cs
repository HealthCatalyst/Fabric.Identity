namespace Fabric.Identity.Client
{
    using System;
    using System.Collections.Generic;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Threading.Tasks;

    using Fabric.Identity.Client.Models;

    using Newtonsoft.Json;

    /// <summary>
    /// Client for registering Client applications and APIs with Fabric.Identity.
    /// </summary>
    public class RegistrationClient : IDisposable
    {
        /// <summary>
        /// The HttpClient used for making Http calls
        /// </summary>
        protected readonly HttpClient HttpClient;

        /// <summary>
        /// Initializes a new instance of the <see cref="RegistrationClient"/> class. 
        /// </summary>
        /// <param name="fabricIdentityBaseUrl">
        /// The base address for Fabric.Identity, e.g. http://host.sample.com/identity 
        /// </param>
        /// <exception cref="UriFormatException">Thrown if the fabricIdentityBaseUrl has an invalid format.</exception>
        public RegistrationClient(string fabricIdentityBaseUrl)
        {
            this.HttpClient = new HttpClient { BaseAddress = new Uri(fabricIdentityBaseUrl) };
            this.HttpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        }

        /// <summary>
        /// Registers an API asynchronously with Fabric.Identity.
        /// </summary>
        /// <param name="accessToken"> The access token to access the registration API. </param>
        /// <param name="apiName"> The API name. </param>
        /// <param name="userClaims"> The list of user claims that should be included on the access token that gets passed to the API. </param>
        /// <param name="scopes"> The list of scopes to register for the API. </param>
        /// <returns> The <see cref="Task"/>. </returns>
        public async Task<ApiResourceResponse> RegisterApiAsync(string accessToken, string apiName, ICollection<string> userClaims, ICollection<string> scopes)
        {
            var apiResourceToPost = new ApiResource(apiName, userClaims, scopes);
            return await this.RegisterApiAsync(accessToken, apiResourceToPost).ConfigureAwait(false);
        }

        /// <summary>
        /// Registers an API asynchronously with Fabric.Identity.
        /// </summary>
        /// <param name="accessToken">The access token to access the registration API.</param>
        /// <param name="apiResource">The <see cref="ApiResource" /> to register.</param>
        /// <returns>The <see cref="Task"/>.</returns>
        public async Task<ApiResourceResponse> RegisterApiAsync(string accessToken, ApiResource apiResource)
        {
            var apiResult = await this.HttpClient.PostAsync(
                                    "/api/apiresource",
                                    new StringContent(JsonConvert.SerializeObject(apiResource)))
                                .ConfigureAwait(false);

            if (!apiResult.IsSuccessStatusCode)
            {
                var error =
                    JsonConvert.DeserializeObject<Error>(
                        await apiResult.Content.ReadAsStringAsync().ConfigureAwait(false));
                return new ApiResourceResponse { IsError = true, Error = error };
            }

            var returnedApiResource =
                JsonConvert.DeserializeObject<ApiResource>(
                    await apiResult.Content.ReadAsStringAsync().ConfigureAwait(false));
            return new ApiResourceResponse { ApiResource = returnedApiResource };
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
                this.HttpClient?.Dispose();
            }
        }
    }
}
