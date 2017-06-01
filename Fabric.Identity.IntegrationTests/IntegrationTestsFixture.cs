using System;
using System.IO;
using System.Net.Http;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;

namespace Fabric.Identity.IntegrationTests
{
    public class IntegrationTestsFixture : IDisposable
    {
        private readonly TestServer _server;

        public IntegrationTestsFixture()
        {
            var builder = new WebHostBuilder()
                .UseKestrel()
                .UseContentRoot(Directory.GetCurrentDirectory())
                .UseStartup<API.Startup>()
                .UseApplicationInsights()
                .UseUrls("http://*:5000");

            _server = new TestServer(builder);

            this.HttpClient = _server.CreateClient();
            this.HttpClient.BaseAddress = new Uri("http://localhost:5000");
        }

        public HttpClient HttpClient { get; }

        #region IDisposable implementation

        // Dispose() calls Dispose(true)
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~IntegrationTestsFixture()
        {
            // Finalizer calls Dispose(false)
            Dispose(false);
        }

        // The bulk of the clean-up code is implemented in Dispose(bool)
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                // free managed resources
                this.HttpClient.Dispose();
                _server.Dispose();
            }
        }

        #endregion IDisposable implementation
    }
}