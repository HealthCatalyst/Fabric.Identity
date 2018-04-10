using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Fabric.Identity.API.Configuration;
using Fabric.Identity.API.Infrastructure;
using Fabric.Identity.API.Models;
using Fabric.Identity.API.Extensions;
using Fabric.Platform.Http;
using Fabric.Platform.Shared.Exceptions;
using IdentityModel.Client;
using Newtonsoft.Json;
using Polly.CircuitBreaker;
using Serilog;

namespace Fabric.Identity.API.Services
{
    public class IdPSearchServiceProvider : IExternalIdentityProviderService
    {
        private readonly IAppConfiguration _appConfig;
        private readonly ILogger _logger;
        private readonly PolicyProvider _policyProvider;
        private readonly HttpClient _httpClient;
        private readonly IHttpRequestMessageFactory _httpRequestMessageFactory;

        public IdPSearchServiceProvider(IAppConfiguration appConfig, ILogger logger, PolicyProvider policyProvider, HttpClient httpClient, IHttpRequestMessageFactory httpRequestMessageFactory)
        {
            _appConfig = appConfig;
            _logger = logger;
            _policyProvider = policyProvider;
            _httpClient = httpClient;
            _httpRequestMessageFactory = httpRequestMessageFactory;
        }

        public async Task<ExternalUser> FindUserBySubjectId(string subjectId)
        {
            if (!_appConfig.IdentityProviderSearchSettings.IsEnabled)
            {
                _logger.Information("Identity provider search service is disabled");
                return null;
            }

            ExternalUser user = null;
            
            try
            {
                user = await _policyProvider.IdPSearchServiceErrorPolicy.Execute(() => SearchForUser(subjectId));
            }
            catch (BrokenCircuitException ex)
            {
                // catch and log the error so we degrade gracefully when we can't connect to the service
                _logger.Error(ex, "Identity Provider Search Service circuit breaker is in an open state, not attempting to connect to the service");
            }

            return user;
        }

        private async Task<ExternalUser> SearchForUser(string subjectId)
        {
            var settings = _appConfig.IdentityServerConfidentialClientSettings;
            const string fabricIdentityClient = "fabric-identity-client";
            if (string.IsNullOrEmpty(settings.Authority))
            {
                throw new FabricConfigurationException(
                    "IdentityServerConfidentialClientSettings.Authority is not set, Please set the Authority to the appropriate url.");
            }

            var authority = settings.Authority.EnsureTrailingSlash();

            var tokenUriAddress = $"{authority}connect/token";
            _logger.Information($"Getting access token for ClientId: {fabricIdentityClient} at {tokenUriAddress}");

            var tokenClient = new TokenClient(tokenUriAddress, fabricIdentityClient, settings.ClientSecret);
            var accessTokenResponse = await tokenClient.RequestClientCredentialsAsync("fabric/idprovider.searchusers");
            if (accessTokenResponse.IsError)
            {
                _logger.Error(
                    $"Failed to get access token, error message is: {accessTokenResponse.ErrorDescription}");
            }

            var baseUri = _appConfig.IdentityProviderSearchSettings.BaseUrl.EnsureTrailingSlash();

            var searchServiceUrl =
                $"{baseUri}{_appConfig.IdentityProviderSearchSettings.GetUserEndpoint.EnsureTrailingSlash()}{subjectId}";

            var httpRequestMessage = _httpRequestMessageFactory.CreateWithAccessToken(HttpMethod.Get,
                new Uri(searchServiceUrl),
                accessTokenResponse.AccessToken);

            httpRequestMessage.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            try
            {
                _logger.Information($"searching for user with url: {searchServiceUrl}");

                var response = await _httpClient.SendAsync(httpRequestMessage);

                var responseContent =
                    response.Content == null ? string.Empty : await response.Content.ReadAsStringAsync();

                if (response.StatusCode != HttpStatusCode.OK)
                {
                    _logger.Error(
                        $"no user principal was found for subject id: {subjectId}. response status code: {response.StatusCode}.");
                    _logger.Error($"response from search service: {responseContent}");
                    return null;
                }

                var result = JsonConvert.DeserializeObject<UserSearchResponse>(responseContent);

                return new ExternalUser
                {
                    FirstName = result.FirstName,
                    LastName = result.LastName,
                    MiddleName = result.MiddleName,
                    SubjectId = result.SubjectId
                };
            }
            catch (HttpRequestException e)
            {
                var baseException = e.GetBaseException();

                _logger.Error($"there was an error connecting to the search service: {baseException.Message}");
                return null;
            }
        }

        public ICollection<ExternalUser> SearchUsers(string searchText)
        {
            throw new NotImplementedException();
        }
    }

    public class UserSearchResponse
    {
        public string SubjectId { get; set; }
        public string FirstName { get; set; }
        public string MiddleName { get; set; }
        public string LastName { get; set; }
        public string PrincipalType { get; set; }
    }
}
