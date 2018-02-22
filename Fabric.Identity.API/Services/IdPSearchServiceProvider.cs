using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using Fabric.Identity.API.Configuration;
using Fabric.Identity.API.Models;
using Fabric.Platform.Http;
using IdentityModel.Client;
using Newtonsoft.Json;
using Serilog;

namespace Fabric.Identity.API.Services
{
    public class IdPSearchServiceProvider : IExternalIdentityProviderService
    {
        private readonly IAppConfiguration _appConfig;
        private readonly ILogger _logger;

        public IdPSearchServiceProvider(IAppConfiguration appConfig, ILogger logger)
        {
            _appConfig = appConfig;
            _logger = logger;
        }

        public ExternalUser FindUserBySubjectId(string subjectId)
        {
            var settings = _appConfig.IdentityServerConfidentialClientSettings;
            var fabricIdentityClient = "fabric-identity-client";

            var tokenUriAddress = $"{settings.Authority}connect/token";
            var tokenClient = new TokenClient(tokenUriAddress, fabricIdentityClient, settings.ClientSecret);
            var accessTokenResponse = tokenClient.RequestClientCredentialsAsync("fabric/idprovider.searchusers").Result;

            var httpClient = new HttpClientFactory(
                tokenUriAddress,
                fabricIdentityClient,
                settings.ClientSecret,
                null,
                null).CreateWithAccessToken(new Uri(_appConfig.IdPSearchServiceUrl), accessTokenResponse.AccessToken);

            _logger.Information($"access token for search service: { accessTokenResponse.AccessToken}");

            var searchServiceUrl = $"/v1/principals/user?subjectid={subjectId}";
            _logger.Information($"searching for user by subject id with url: {searchServiceUrl}");            

            var response = httpClient.GetAsync(searchServiceUrl).Result;            

            var responseContent = response.Content == null ? string.Empty : response.Content.ReadAsStringAsync().Result;

            if (string.IsNullOrEmpty(responseContent) || response.StatusCode != HttpStatusCode.OK)
            {                
                _logger.Information($"no user principal was found for subject id: {subjectId}. response status code: {response.StatusCode}");
                return null;
            }

            _logger.Information($"response content from the search service: {responseContent}");

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
