using System;
using System.Collections.Generic;
using System.Text;

namespace Fabric.Identity.Client
{
    using System.Net.Http;

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
