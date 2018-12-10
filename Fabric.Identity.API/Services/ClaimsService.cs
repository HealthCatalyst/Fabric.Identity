using Fabric.Identity.API.Configuration;
using Fabric.Identity.API.Exceptions;
using Fabric.Identity.API.Models;
using IdentityModel;
using IdentityServer4.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http.Authentication;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Claims;
using static Fabric.Identity.API.FabricIdentityConstants;

namespace Fabric.Identity.API.Services
{
    public class ClaimsService : IClaimsService
    {
        private readonly IAppConfiguration _appConfiguration;

        public ClaimsService(
            IAppConfiguration appConfiguration)
        {
            _appConfiguration = appConfiguration;
        }

        public ClaimsResult GenerateClaimsForIdentity(AuthenticateInfo info, AuthorizationRequest context)
        {
            CheckWhetherArgumentIsNull(info, nameof(info));
            // NOTE: context may be null.  because of this, we are not going to 
            // validate context
            
            ClaimsResult result = GenerateNewClaimsResult(info, context);

            if (this.IsExternalTokenAzureAD(result.SchemeItem))
            {
                this.ValidateAzureADExternalToken(result);
            }

            //remove the user id claim from the claims collection and move to the userId property
            //also set the name of the external authentication provider
            result.Claims.Remove(result.UserIdClaim);

            //get the client id from the auth context
            result.AdditionalClaims = this.GenerateAdditionalClaims(result.Claims);
            result.AuthenticationProperties = this.GenerateAuthenticationProperties(info);

            return result;
        }

        private ClaimsResult GenerateNewClaimsResult(AuthenticateInfo info, AuthorizationRequest context)
        {
            // provider and scheme look the same, but if you see the values
            //  FabricIdentityConstants.AuthenticationSchemes.Azure = "OpenIdConnect:CatalystAzureAD"
            // you will notice there are 2 different values, one for the provider and the other for the scheme
            var provider = info.Properties.Items["scheme"];
            var schemeItem = info.Properties.Items.FirstOrDefault(i => i.Key == "scheme").Value;
            var externalUser = info?.Principal;
            if (externalUser == null)
            {
                throw new Exception("External authentication error");
            }

            var claims = externalUser.Claims.ToList();
            var userIdClaim = this.GetUserIdClaim(claims);
            return new ClaimsResult()
            {
                ClientId = context?.ClientId,
                UserId = userIdClaim.Value,
                Provider = provider,
                SchemeItem = schemeItem,
                Claims = claims,
                UserIdClaim = userIdClaim
            };
        }

        public string GetEffectiveSubjectId(ClaimsResult claimInformation, User user)
        {
            CheckWhetherArgumentIsNull(user, nameof(user));
            CheckWhetherArgumentIsNull(claimInformation, nameof(claimInformation));

            string subjectId = null;
            if (this.IsExternalTokenAzureAD(claimInformation.SchemeItem))
            {
                subjectId = claimInformation.Claims.FirstOrDefault(x => x.Type == AzureActiveDirectoryJwtClaimTypes.OID || x.Type == AzureActiveDirectoryJwtClaimTypes.OID_Alternative)?
                                  .Value;
            }

            if(subjectId == null)
            {
                subjectId = user?.SubjectId;
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

        private void ValidateAzureADExternalToken(ClaimsResult claimsInformation)
        {
            var issuerClaim = claimsInformation.Claims.FirstOrDefault(x => x.Type == JwtClaimTypes.Issuer);
            if (issuerClaim == null)
            {
                throw new MissingIssuerClaimException(ExceptionMessageResources.MissingIssuerClaimMessage);
            }

            if(!this._appConfiguration.AzureActiveDirectorySettings.IssuerWhiteList.Contains(issuerClaim.Issuer))
            {
                var exception = new InvalidIssuerException(
                    String.Format(CultureInfo.CurrentCulture, ExceptionMessageResources.ForbiddenIssuerMessageUser, issuerClaim?.Value))
                {
                    LogMessage = ExceptionMessageResources.ForbiddenIssuerMessageLog
                };

                throw exception;
            }
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

        private bool IsExternalTokenAzureAD(string schemeItem) => 
            _appConfiguration.AzureAuthenticationEnabled && schemeItem == FabricIdentityConstants.AuthenticationSchemes.Azure;

        private static void CheckWhetherArgumentIsNull(object objectToCheck, string nameOfObject)
        {
            if(objectToCheck == null)
            {
                throw new ArgumentNullException(nameOfObject,
                    string.Format(AuthenticationExceptionMessages.ArgumentNullExceptionMessage, nameOfObject));
            }
        }
    }
}
