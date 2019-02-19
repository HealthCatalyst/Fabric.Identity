using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
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
        private readonly Task _idPSSInitializationTask;

        public IdPSearchServiceProvider(
            IAppConfiguration appConfig,
            ILogger logger,
            PolicyProvider policyProvider,
            HttpClient httpClient,
            IHttpRequestMessageFactory httpRequestMessageFactory,
            IdPSearchServiceUrlInitializer idPSearchServiceUrlInitializer)
        {
            _appConfig = appConfig;
            _logger = logger;
            _policyProvider = policyProvider;
            _httpClient = httpClient;
            _httpRequestMessageFactory = httpRequestMessageFactory;
            _idPSSInitializationTask = idPSearchServiceUrlInitializer.InitializeAsync();
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
                user = await _policyProvider.IdPSearchServicePolicy.ExecuteAsync(() => SearchForUser(subjectId));
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
            if (string.IsNullOrEmpty(settings.Authority))
            {
                throw new FabricConfigurationException(
                    "IdentityServerConfidentialClientSettings.Authority is not set, Please set the Authority to the appropriate url.");
            }

            var authority = settings.Authority.EnsureTrailingSlash();

            var tokenUriAddress = $"{authority}connect/token";
            _logger.Information($"Getting access token for ClientId: {FabricIdentityConstants.FabricIdentityClient} at {tokenUriAddress}");

            var tokenClient = new TokenClient(tokenUriAddress, FabricIdentityConstants.FabricIdentityClient, settings.ClientSecret);
            var accessTokenResponse = await tokenClient.RequestClientCredentialsAsync("fabric/idprovider.searchusers");
            if (accessTokenResponse.IsError)
            {
                _logger.Error(
                    $"Failed to get access token, error message is: {accessTokenResponse.ErrorDescription}");
            }

            await _idPSSInitializationTask;
            var baseUri = _appConfig.IdentityProviderSearchSettings.BaseUrl;

            var searchServiceUrl =
                $"{baseUri}{_appConfig.IdentityProviderSearchSettings.GetUserEndpoint}{subjectId}";

            var httpRequestMessage = _httpRequestMessageFactory.CreateWithAccessToken(HttpMethod.Get,
                new Uri(searchServiceUrl),
                accessTokenResponse.AccessToken);

            httpRequestMessage.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            try
            {
                _logger.Information($"searching for user with url: {searchServiceUrl}");

                var cancellationTokenSource = new CancellationTokenSource(5000);
                var cancellationToken = cancellationTokenSource.Token;
                var response = await _httpClient.SendAsync(httpRequestMessage, cancellationToken);

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
            catch (TaskCanceledException e)
            {
                _logger.Error($"The request to the search service was canceled: {e.Message}");
                throw;
            }
            catch (HttpRequestException e)
            {
                var baseException = e.GetBaseException();

                _logger.Error($"There was an error connecting to the search service: {baseException.Message}");
                throw;
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

    public class IdPSearchServiceUrlInitializer
    {
        private static string _effectiveBaseUrl;
        private readonly IAppConfiguration _appConfiguration;
        private readonly ILogger _logger;

        public IdPSearchServiceUrlInitializer(IAppConfiguration appConfiguration, ILogger logger)
        {
            _appConfiguration = appConfiguration;
            _logger = logger;
        }

        public async Task InitializeAsync()
        {
            if (!string.IsNullOrEmpty(_effectiveBaseUrl)) return;

            _effectiveBaseUrl = await GetBaseUrlAsync();
            _appConfiguration.IdentityProviderSearchSettings.BaseUrl = _effectiveBaseUrl;
            _logger.Information($"Initialized _appConfiguration.IdentityProviderSearchSettings.BaseUrl to {_appConfiguration.IdentityProviderSearchSettings.BaseUrl}");
        }

        private async Task<string> GetBaseUrlAsync()
        {
            if (!_appConfiguration.UseDiscoveryService)
            {
                return _appConfiguration.IdentityProviderSearchSettings.BaseUrl.EnsureTrailingSlash();
            }

            using (var discoveryServiceClient = new DiscoveryServiceClient(_appConfiguration.DiscoveryServiceEndpoint))
            {
                var serviceRegistration = await discoveryServiceClient.GetServiceAsync("IdentityProviderSearchService", 1);
                return !string.IsNullOrEmpty(serviceRegistration?.ServiceUrl)
                    ? serviceRegistration.ServiceUrl.EnsureTrailingSlash()
                    : _appConfiguration.IdentityProviderSearchSettings.BaseUrl.EnsureTrailingSlash();
            }
        }
    }
}
