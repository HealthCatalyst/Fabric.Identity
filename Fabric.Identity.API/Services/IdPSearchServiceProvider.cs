using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Fabric.Identity.API.Configuration;
using Fabric.Identity.API.Infrastructure;
using Fabric.Identity.API.Models;
using Fabric.Platform.Http;
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

        public IdPSearchServiceProvider(IAppConfiguration appConfig, ILogger logger, PolicyProvider policyProvider)
        {
            _appConfig = appConfig;
            _logger = logger;
            _policyProvider = policyProvider;
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
            var fabricIdentityClient = "fabric-identity-client";

            var tokenUriAddress = $"{settings.Authority}connect/token";
            var tokenClient = new TokenClient(tokenUriAddress, fabricIdentityClient, settings.ClientSecret);
            var accessTokenResponse = await tokenClient.RequestClientCredentialsAsync("fabric/idprovider.searchusers");

            var httpClient = new HttpClientFactory(
                tokenUriAddress,
                fabricIdentityClient,
                settings.ClientSecret,
                null,
                null).CreateWithAccessToken(new Uri(_appConfig.IdentityProviderSearchSettings.IdPSearchServiceUrl), accessTokenResponse.AccessToken);

            httpClient.DefaultRequestHeaders.Add("Accept", "application/json");

            var searchServiceUrl = $"/v1/principals/user?subjectid={subjectId}";

            var response = await httpClient.GetAsync(searchServiceUrl);

            var responseContent = response.Content == null ? string.Empty : await response.Content.ReadAsStringAsync();

            if (string.IsNullOrEmpty(responseContent) || response.StatusCode != HttpStatusCode.OK)
            {
                _logger.Information($"no user principal was found for subject id: {subjectId}. response status code: {response.StatusCode}");
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
