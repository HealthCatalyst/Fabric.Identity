using System;
using System.Collections.Generic;
using System.Linq;
using FluentValidation.Results;
using IdentityServer4.Test;
using Newtonsoft.Json;
using IS4 = IdentityServer4.Models;

namespace Fabric.Identity.API.Models
{
    public static class ModelExtensions
    {
        public static Error ToError(this ValidationResult validationResult)
        {
            var details = validationResult.Errors.Select(validationResultError => new Error
            {
                Code = validationResultError.ErrorCode,
                Message = validationResultError.ErrorMessage,
                Target = validationResultError.PropertyName
            })
                .ToList();

            var error = new Error
            {
                Message = details.Count > 1 ? "Multiple Errors" : details.FirstOrDefault().Message,
                Details = details.ToArray()
            };

            return error;
        }

        public static Client ToClientViewModel(this IS4.Client client)
        {
            var newClient = new Client()
            {
                ClientId = client.ClientId,
                ClientName = client.ClientName,
                AllowedScopes = client.AllowedScopes,
                AllowedGrantTypes = client.AllowedGrantTypes,
                AllowedCorsOrigins = client.AllowedCorsOrigins,
                AllowOfflineAccess = client.AllowOfflineAccess,
                RequireConsent = client.RequireConsent,
                RedirectUris = client.RedirectUris,
                PostLogoutRedirectUris = client.PostLogoutRedirectUris
            };

            return newClient;
        }

        public static ApiResource ToApiResourceViewModel(this IS4.ApiResource resource)
        {
            var newResource = new ApiResource()
            {
                Name = resource.Name,
                DisplayName = resource.DisplayName,
                Description = resource.Description,
                Enabled = resource.Enabled,
                UserClaims = new List<string>(resource.UserClaims),
                Scopes = resource.Scopes.Select(s => s.ToScopeViewModel()).ToList()
            };

            return newResource;
        }

        public static Scope ToScopeViewModel(this IS4.Scope scope)
        {
            var newScope = new Scope()
            {
                Name = scope.Name,
                DisplayName = scope.DisplayName,
                Description = scope.Description,
                Emphasize = scope.Emphasize,
                Required = scope.Required,
                UserClaims = new List<string>(scope.UserClaims),
                ShowInDiscoveryDocument = scope.ShowInDiscoveryDocument
            };

            return newScope;
        }

        public static User ToUser(this TestUser testUser)
        {
            return new User
            {
                SubjectId = testUser.SubjectId,
                ProviderName = testUser.ProviderName,
                Username = testUser.Username,
                Claims = testUser.Claims,
            };
        }

        public static UserApiModel ToUserViewModel(this User user, string clientId)
        {
            return new UserApiModel()
            {
                SubjectId = user.SubjectId,
                FirstName = user.FirstName,
                LastName = user.LastName,
                MiddleName = user.MiddleName,
                LastLoginDate = user.LastLoginDatesByClient
                    .SingleOrDefault(l => l.Key.Equals(clientId, StringComparison.OrdinalIgnoreCase))
                    .Value
            };
        }

        public static IEnumerable<T> Deserialize<T>(this IEnumerable<string> jsonObjects)
        {
            var documentList = new List<T>();
            foreach (var document in jsonObjects)
            {
                documentList.Add(JsonConvert.DeserializeObject<T>(document, new SerializationSettings().JsonSettings));
            }
            return documentList;
        }
    }
}