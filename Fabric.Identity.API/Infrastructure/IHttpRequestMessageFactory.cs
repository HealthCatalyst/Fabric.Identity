using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace Fabric.Identity.API.Infrastructure
{
    public interface IHttpRequestMessageFactory
    {
        Task<HttpRequestMessage> Create(HttpMethod httpMethod, Uri uri, string requestScope);
        HttpRequestMessage CreateWithAccessToken(HttpMethod httpMethod, Uri uri, string accessToken);
    }
}
