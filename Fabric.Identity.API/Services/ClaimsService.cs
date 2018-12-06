using Fabric.Identity.API.Configuration;
using Fabric.Identity.API.Exceptions;
using Fabric.Identity.API.Models;
using Fabric.Identity.API.Quickstart.Account;
using IdentityModel;
using IdentityServer4.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http.Authentication;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Claims;

namespace Fabric.Identity.API.Services
{
    public class ClaimsService
    {
        private readonly IAppConfiguration _appConfiguration;

        public ClaimsService(
            IAppConfiguration appConfiguration)
        {
            _appConfiguration = appConfiguration;
        }

        public ClaimsResult GenerateClaimsForIdentity(List<Claim> claims, AuthenticateInfo info, AuthorizationRequest context, string schemaItem)
        {
            var userIdClaim = this.GetUserIdClaim(claims);
            var provider = info.Properties.Items["scheme"];
            var result = new ClaimsResult()
            {
                ClientId = context?.ClientId,
                UserId = userIdClaim.Value,
                Provider = provider
            };

            if (this.IsAzureEnabled(schemaItem))
            {
                if (this.ValidateAzureAD(claims))
                {
                    var issuerClaim = claims.FirstOrDefault(x => x.Type == JwtClaimTypes.Issuer);
                    var exception = new InvalidIssuerException(
                        String.Format(CultureInfo.CurrentCulture, ExceptionMessageResources.ForbiddenIssuerMessageUser, issuerClaim?.Value))
                    {
                        LogMessage = ExceptionMessageResources.ForbiddenIssuerMessageLog
                    };

                    throw exception;
                }
            }

            //remove the user id claim from the claims collection and move to the userId property
            //also set the name of the external authentication provider
            claims.Remove(userIdClaim);

            //get the client id from the auth context
            result.AdditionalClaims = this.GenerateAdditionalClaims(claims);
            result.AuthenticationProperties = this.GenerateAuthenticationProperties(info);

            return result;
        }

        public string GetSubjectId(List<Claim> claims, User user, string schemaItem)
        {
            string subjectId = null;
            if (this.IsAzureEnabled(schemaItem))
            {
                subjectId = claims.FirstOrDefault(x => x.Type == AzureActiveDirectoryJwtClaimTypes.OID || x.Type == AzureActiveDirectoryJwtClaimTypes.OID_Alternative)
                                  .Value;
            }

            if(subjectId == null)
            {
                subjectId = user.SubjectId;
            }

            return subjectId;
        }

        private Claim[] GenerateAdditionalClaims(List<Claim> previousClaims)
        {
            var additionalClaims = new List<Claim>();
            //if the external system sent a session id claim, copy it over
            var sid = previousClaims.FirstOrDefault(x => x.Type == JwtClaimTypes.SessionId);
            if (sid != null)
            {
                additionalClaims.Add(new Claim(JwtClaimTypes.SessionId, sid.Value));
            }

            //if the external provider issues groups claims, copy it over
            var groupClaims = previousClaims.Where(c => c.Type == "groups").ToList();
            if (groupClaims.Any())
            {
                additionalClaims.AddRange(groupClaims);
            }

            return additionalClaims.ToArray();
        }

        private AuthenticationProperties GenerateAuthenticationProperties(AuthenticateInfo info)
        {
            //if the external provider issued an id_token, we'll keep it for signout
            AuthenticationProperties props = null;
            var id_token = info.Properties.GetTokenValue("id_token");
            if (id_token != null)
            {
                props = new AuthenticationProperties();
                props.StoreTokens(new[] { new AuthenticationToken { Name = "id_token", Value = id_token } });
            }

            return props;
        }

        private bool ValidateAzureAD(List<Claim> claims)
        {
            var issuerClaim = claims.FirstOrDefault(x => x.Type == JwtClaimTypes.Issuer);
            if (issuerClaim == null)
            {
                throw new MissingIssuerClaimException(ExceptionMessageResources.MissingIssuerClaimMessage);
            }

            return !this._appConfiguration.AzureActiveDirectorySettings.IssuerWhiteList.Contains(issuerClaim.Issuer);
        }

        private Claim GetUserIdClaim(List<Claim> claims)
        {
            //try to determine the unique id of the external user - the most common claim type for that are the sub claim and the NameIdentifier
            //depending on the external provider, some other claim type might be used
            var userIdClaim = claims.FirstOrDefault(x => x.Type == JwtClaimTypes.Subject);
            if (userIdClaim == null)
            {
                userIdClaim = claims.FirstOrDefault(x => x.Type == ClaimTypes.NameIdentifier);
            }

            if (userIdClaim == null)
            {
                throw new MissingUserClaimException(ExceptionMessageResources.MissingUserClaimMessage);
            }

            return userIdClaim;
        }

        private bool IsAzureEnabled(string schemaItem) => 
            _appConfiguration.AzureAuthenticationEnabled && schemaItem == FabricIdentityConstants.AuthenticationSchemes.Azure;
    }
}
