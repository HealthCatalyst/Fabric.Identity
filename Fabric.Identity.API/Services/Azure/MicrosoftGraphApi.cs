using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fabric.Identity.API.Configuration;
using Fabric.Identity.API.Models;
using IdentityModel.Client;
using Microsoft.Graph;
using User = Fabric.Identity.API.Models.User;

namespace Fabric.Identity.API.Services.Azure
{
    public class MicrosoftGraphApi : IMicrosoftGraphApi
    {
        private static IDictionary<string, AzureClientApplicationSettings> _appSettings;
        private readonly IAzureActiveDirectoryClientCredentialsService _azureActiveDirectoryClientCredentialsService;
        private IAppConfiguration _appConfiguration;

        public MicrosoftGraphApi(IAppConfiguration appConfiguration, IAzureActiveDirectoryClientCredentialsService settingsService)
        {
            _appSettings = AzureClientApplicationSettings.CreateDictionary(appConfiguration.AzureActiveDirectoryClientSettings);
            _azureActiveDirectoryClientCredentialsService = settingsService;
            _appConfiguration = appConfiguration;
        }

        public async Task<FabricGraphApiUser> GetUserAsync(string subjectId, string tenantId = null)
        {
            if (!string.IsNullOrWhiteSpace(tenantId))
            {
                return await GetUserByTenantAsync(subjectId, tenantId);
            }

            foreach (var key in _appSettings.Keys)
            {
                var user = await GetUserByTenantAsync(subjectId, key);
                if (user != null)
                {
                    return user;
                }
            }

            return null;
        }

        private async Task<FabricGraphApiUser> GetUserByTenantAsync(string subjectId, string tenantId)
        {
            var token = await _azureActiveDirectoryClientCredentialsService.GetAzureAccessTokenAsync(tenantId);
            if (token != null)
            {
                var client = GetNewClient(token);
                AzureClientApplicationSettings setting;
                _appSettings.TryGetValue(tenantId, out setting);
                var apiUser = await client.Users[subjectId].Request().GetAsync().ConfigureAwait(false);
                if (apiUser != null)
                {
                    FabricGraphApiUser user = new FabricGraphApiUser(apiUser)
                    {
                        TenantId = tenantId,
                        TenantAlias = setting.TenantAlias

                    };
                    return user;
                }
            }

            return null;
        }

        public async Task<IEnumerable<FabricGraphApiUser>> GetUserCollectionsAsync(string filterQuery, string tenantId = null)
        {
            if (!string.IsNullOrWhiteSpace(tenantId))
            {
                return await GetUserCollectionsByTenantAsync(filterQuery, tenantId);
            }

            var searchTasks = new List<Task<IEnumerable<FabricGraphApiUser>>>();

            foreach (var key in _appSettings.Keys)
            {
                var tempTask = GetUserCollectionsByTenantAsync(filterQuery, key);
                if (tempTask != null)
                {
                    searchTasks.Add(tempTask);
                }
            }

            var results = await Task.WhenAll(searchTasks).ConfigureAwait(false);
            return results.SelectMany(result => result);
        }

        private async Task<IEnumerable<FabricGraphApiUser>> GetUserCollectionsByTenantAsync(string filterQuery, string tenantId)
        {
            var token = await _azureActiveDirectoryClientCredentialsService.GetAzureAccessTokenAsync(tenantId);
            if (token != null)
            {
                var client = GetNewClient(token);
                AzureClientApplicationSettings setting;
                _appSettings.TryGetValue(tenantId, out setting);
                var userPages = await client.Users.Request().Filter(filterQuery).GetAsync();
                return userPages.Select(user => new FabricGraphApiUser(user as Microsoft.Graph.User)
                {
                    TenantId = tenantId,
                    TenantAlias = setting.TenantAlias
                });
            }

            return null;
        }

        public async Task<IEnumerable<FabricGraphApiGroup>> GetGroupCollectionsAsync(string filterQuery, string tenantId = null)
        {
            if (!string.IsNullOrWhiteSpace(tenantId))
            {
                return await GetGroupCollectionsByTenantAsync(filterQuery, tenantId);
            }

            var searchTasks = new List<Task<IEnumerable<FabricGraphApiGroup>>>();

            foreach (var key in _appSettings.Keys)
            {
                var tempTask = GetGroupCollectionsByTenantAsync(filterQuery, key);
                if (tempTask != null)
                {
                    searchTasks.Add(tempTask);
                }
            }

            var results = await Task.WhenAll(searchTasks).ConfigureAwait(false);
            return results.SelectMany(result => result);
        }

        private async Task<IEnumerable<FabricGraphApiGroup>> GetGroupCollectionsByTenantAsync(string filterQuery, string tenantId)
        {
            var token = await _azureActiveDirectoryClientCredentialsService.GetAzureAccessTokenAsync(tenantId);
            if (token != null)
            {
                var client = GetNewClient(token);
                AzureClientApplicationSettings setting;
                _appSettings.TryGetValue(tenantId, out setting);
                var groupPages = await client.Groups.Request().Filter(filterQuery).GetAsync();
                return groupPages.Select(group => new FabricGraphApiGroup(group as Group)
                {
                    TenantId = tenantId,
                    TenantAlias = setting.TenantAlias
                });
            }

            return null;
        }

        private IGraphServiceClient GetNewClient(TokenResponse token)
        {
            return new GraphServiceClient(new DelegateAuthenticationProvider((requestMessage) =>
            {
                requestMessage
                    .Headers
                    .Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("bearer", token.AccessToken);

                return Task.FromResult(0);
            }));
        }
    }
}
