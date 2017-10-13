using System;
using System.Collections.Generic;
using System.Linq;
using Fabric.Identity.API.Exceptions;
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

        public static IS4.Client ToIs4ClientModel(this Client client)
        {
            if (client == null)
            {
                return null;
            }

            try
            {
                var newClient = new IS4.Client
                {
                    // Basic
                    Enabled = client.Enabled,
                    ClientId = client.ClientId,
                    RequireClientSecret = client.RequireClientSecret,
                    AllowedGrantTypes = client.AllowedGrantTypes,
                    RequirePkce = client.RequirePkce,
                    AllowPlainTextPkce = client.AllowPlainTextPkce,
                    RedirectUris = client.RedirectUris,
                    AllowedScopes = client.AllowedScopes,
                    AllowOfflineAccess = client.AllowOfflineAccess,
                    AllowAccessTokensViaBrowser = client.AllowAccessTokensViaBrowser,
                    ProtocolType = client.ProtocolType,

                    // Authentication / Logout
                    PostLogoutRedirectUris = client.PostLogoutRedirectUris,
                    EnableLocalLogin = client.EnableLocalLogin,
                    IdentityProviderRestrictions = client.IdentityProviderRestrictions,
                    LogoutUri = client.LogoutUri,
                    LogoutSessionRequired = client.LogoutSessionRequired,

                    // Token
                    IdentityTokenLifetime = client.IdentityTokenLifetime,
                    AccessTokenLifetime = client.AccessTokenLifetime,
                    AuthorizationCodeLifetime = client.AuthorizationCodeLifetime,
                    AbsoluteRefreshTokenLifetime = client.AbsoluteRefreshTokenLifetime,
                    SlidingRefreshTokenLifetime = client.SlidingRefreshTokenLifetime,
                    RefreshTokenUsage = client.RefreshTokenUsage,
                    RefreshTokenExpiration = client.RefreshTokenExpiration,
                    UpdateAccessTokenClaimsOnRefresh = client.UpdateAccessTokenClaimsOnRefresh,
                    AccessTokenType = client.AccessTokenType,
                    IncludeJwtId = client.IncludeJwtId,
                    AllowedCorsOrigins = client.AllowedCorsOrigins,
                    Claims = client.Claims,
                    AlwaysSendClientClaims = client.AlwaysSendClientClaims,
                    AlwaysIncludeUserClaimsInIdToken = client.AlwaysIncludeUserClaimsInIdToken,
                    PrefixClientClaims = client.PrefixClientClaims,

                    // Consent
                    RequireConsent = client.RequireConsent,
                    AllowRememberConsent = client.AllowRememberConsent,
                    ClientName = client.ClientName,
                    ClientUri = client.ClientUri,
                    LogoUri = client.LogoUri
                };

                return newClient;
            }
            catch (InvalidOperationException ex)
            {
                throw new BadRequestException<Client>(client, ex.Message);
            }
        }

        public static Client ToClientViewModel(this IS4.Client client)
        {
            if (client == null)
            {
                return null;
            }

            var newClient = new Client
            {
                // Basic
                Enabled = client.Enabled,
                ClientId = client.ClientId,
                RequireClientSecret = client.RequireClientSecret,
                AllowedGrantTypes = client.AllowedGrantTypes,
                RequirePkce = client.RequirePkce,
                AllowPlainTextPkce = client.AllowPlainTextPkce,
                RedirectUris = client.RedirectUris,
                AllowedScopes = client.AllowedScopes,
                AllowOfflineAccess = client.AllowOfflineAccess,
                AllowAccessTokensViaBrowser = client.AllowAccessTokensViaBrowser,
                ProtocolType = client.ProtocolType,

                // Authentication / Logout
                PostLogoutRedirectUris = client.PostLogoutRedirectUris,
                EnableLocalLogin = client.EnableLocalLogin,
                IdentityProviderRestrictions = client.IdentityProviderRestrictions,
                LogoutUri = client.LogoutUri,
                LogoutSessionRequired = client.LogoutSessionRequired,
                
                // Token
                IdentityTokenLifetime = client.IdentityTokenLifetime,
                AccessTokenLifetime = client.AccessTokenLifetime,
                AuthorizationCodeLifetime = client.AuthorizationCodeLifetime,
                AbsoluteRefreshTokenLifetime = client.AbsoluteRefreshTokenLifetime,
                SlidingRefreshTokenLifetime = client.SlidingRefreshTokenLifetime,
                RefreshTokenUsage = client.RefreshTokenUsage,
                RefreshTokenExpiration = client.RefreshTokenExpiration,
                UpdateAccessTokenClaimsOnRefresh = client.UpdateAccessTokenClaimsOnRefresh,
                AccessTokenType = client.AccessTokenType,
                IncludeJwtId = client.IncludeJwtId,
                AllowedCorsOrigins = client.AllowedCorsOrigins,
                Claims = client.Claims,
                AlwaysSendClientClaims = client.AlwaysSendClientClaims,
                AlwaysIncludeUserClaimsInIdToken = client.AlwaysIncludeUserClaimsInIdToken,
                PrefixClientClaims = client.PrefixClientClaims,

                // Consent
                RequireConsent = client.RequireConsent,
                AllowRememberConsent = client.AllowRememberConsent,
                ClientName = client.ClientName,
                ClientUri = client.ClientUri,
                LogoUri = client.LogoUri
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
            var lastLoginDate = user.LastLoginDatesByClient
                .SingleOrDefault(l => l.Key.Equals(clientId, StringComparison.OrdinalIgnoreCase))
                .Value;

            DateTime? dateToSet = null;
            if (lastLoginDate != DateTime.MinValue)
            {
                dateToSet = lastLoginDate;
            }

            return new UserApiModel()
            {
                SubjectId = user.SubjectId,
                FirstName = user.FirstName,
                LastName = user.LastName,
                MiddleName = user.MiddleName,
                LastLoginDate = dateToSet
            };
        }

        public static IEnumerable<T> Deserialize<T>(this IEnumerable<string> jsonObjects)
        {
            var documentList = new List<T>();
            foreach (var document in jsonObjects)
            {
                if (document == null)
                {
                    continue;
                }

                documentList.Add(JsonConvert.DeserializeObject<T>(document, new SerializationSettings().JsonSettings));
            }
            return documentList;
        }
    }
}