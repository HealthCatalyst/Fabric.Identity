using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Fabric.Identity.UnitTests.Mocks
{

    public class MockHttpHandler : HttpMessageHandler
    {
        public virtual HttpResponseMessage Send(HttpRequestMessage request)
        {
            throw new NotImplementedException("You must setup a mock for this method.");
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(Send(request));
        }
    }
}
