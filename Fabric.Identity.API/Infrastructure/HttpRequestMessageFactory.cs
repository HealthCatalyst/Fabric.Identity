using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using IdentityModel.Client;
using Fabric.Platform.Shared;


namespace Fabric.Identity.API.Infrastructure
{
    public class HttpRequestMessageFactory : IHttpRequestMessageFactory
    {
        private readonly string _correlationToken;
        private readonly string _subject;
        private readonly HttpClient _httpClient;
        private readonly string _tokenUrl;
        private readonly string _clientId;
        private readonly string _secret;

        public HttpRequestMessageFactory(string tokenUrl, string clientId, string secret, string correlationToken, string subject)
        {
            _correlationToken = correlationToken;
            _subject = subject;
            _httpClient = new HttpClient();
            _tokenUrl = tokenUrl;
            _clientId = clientId;
            _secret = secret;
        }
        public async Task<HttpRequestMessage> Create(HttpMethod httpMethod, Uri uri, string requestScope)
        {
            var tokenRequest = new ClientCredentialsTokenRequest
            {
                Address = _tokenUrl,
                ClientId = _clientId,
                ClientSecret = _secret,
                Scope = requestScope
            };
            var response = await _httpClient.RequestClientCredentialsTokenAsync(tokenRequest).ConfigureAwait(false);
            return CreateWithAccessToken(httpMethod, uri, response.AccessToken);
        }

        public HttpRequestMessage CreateWithAccessToken(HttpMethod httpMethod, Uri uri, string accessToken)
        {
            var httpRequestMessage = new HttpRequestMessage(httpMethod, uri);
            httpRequestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            httpRequestMessage.Headers.Add(Constants.FabricHeaders.CorrelationTokenHeaderName, _correlationToken);
            if (!string.IsNullOrEmpty(_subject))
            {
                httpRequestMessage.Headers.Add(Constants.FabricHeaders.SubjectNameHeader, _subject);
            }
            return httpRequestMessage;
        }
    }
}
