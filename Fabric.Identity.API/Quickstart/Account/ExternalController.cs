using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Security.Principal;
using System.Threading.Tasks;
using Fabric.Identity.API.Configuration;
using Fabric.Identity.API.Events;
using Fabric.Identity.API.Exceptions;
using Fabric.Identity.API.Management;
using Fabric.Identity.API.Models;
using Fabric.Identity.API.Services;
using IdentityModel;
using IdentityServer4.Events;
using IdentityServer4.Quickstart.UI;
using IdentityServer4.Services;
using IdentityServer4.Stores;
using IdentityServer4.Test;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Serilog;

namespace Host.Quickstart.Account
{
    [SecurityHeaders]
    [AllowAnonymous]
    public class ExternalController : Controller
    {
        private readonly TestUserStore _users;
        private readonly IIdentityServerInteractionService _interaction;
        private readonly IClientStore _clientStore;
        private readonly IEventService _events;
        private readonly GroupFilterService _groupFilterService;
        private readonly IExternalIdentityProviderService _externalIdentityProviderService;
        private readonly Fabric.Identity.API.Services.IClaimsService _claimsService;
        private readonly UserLoginManager _userLoginManager;
        private readonly ILogger _logger;


        public ExternalController(
            IIdentityServerInteractionService interaction,
            IClientStore clientStore,
            IEventService events,
            GroupFilterService groupFilterService,
            IExternalIdentityProviderService externalIdentityProviderService,
            Fabric.Identity.API.Services.IClaimsService claimsService,
            UserLoginManager userLoginManager,
            IAppConfiguration appConfiguration,
            ILogger logger,
            TestUserStore users = null)
        {
            _users = users ?? MakeTestUserStore(appConfiguration);

            _interaction = interaction;
            _clientStore = clientStore;
            _events = events;
            _groupFilterService = groupFilterService;
            _externalIdentityProviderService = externalIdentityProviderService;
            _claimsService = claimsService;
            _userLoginManager = userLoginManager;
            _logger = logger;
        }

        /// <summary>
        /// initiate roundtrip to external authentication provider
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Challenge(string provider, string returnUrl)
        {
            if (string.IsNullOrEmpty(returnUrl)) returnUrl = "~/";

            // validate returnUrl - either it is a valid OIDC URL or back to a local page
            if (Url.IsLocalUrl(returnUrl) == false && _interaction.IsValidReturnUrl(returnUrl) == false)
            {
                // user might have clicked on a malicious link - should be logged
                throw new Exception("invalid return URL");
            }

            if (AccountOptions.WindowsAuthenticationSchemeName == provider)
            {
                // windows authentication needs special handling
                return await ProcessWindowsLoginAsync(returnUrl);
            }
            else
            {
                // start challenge and roundtrip the return URL and scheme 
                var props = new AuthenticationProperties
                {
                    RedirectUri = Url.Action(nameof(Callback)),
                    Items =
                    {
                        { "returnUrl", returnUrl },
                        { "scheme", provider },
                    }
                };

                return Challenge(props, provider);
            }
        }

        /// <summary>
        /// Post processing of external authentication
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Callback()
        {
            // read external identity from the temporary cookie
            var result = await HttpContext.AuthenticateAsync(IdentityServer4.IdentityServerConstants.ExternalCookieAuthenticationScheme);
            if (result?.Succeeded != true)
            {
                throw new Exception("External authentication error");
            }
            // retrieve return URL
            var returnUrl = result.Properties.Items["returnUrl"] ?? "~/";

            // check if external login is in the context of an OIDC request
            var context = await _interaction.GetAuthorizationContextAsync(returnUrl);

            //retrieve claims of the external user
            ClaimsResult claimInformation = null;

            try
            {
                claimInformation = _claimsService.GenerateClaimsForIdentity(result, context);
            }
            catch (InvalidIssuerException exc)
            {
                return LogAndReturnStatus(403, exc.LogMessage, exc.Message);
            }

            //// lookup our user and external provider info
            //var (user, provider, providerUserId, claims) = FindUserFromExternalProvider(result);
            //if (user == null)
            //{
            //    // this might be where you might initiate a custom workflow for user registration
            //    // in this sample we don't show how that would be done, as our sample implementation
            //    // simply auto-provisions new external user
            //    user = AutoProvisionUser(provider, providerUserId, claims);
            //}

            var user = await _userLoginManager.UserLogin(
                claimInformation.Provider,
                _claimsService.GetEffectiveUserId(claimInformation),
                claimInformation.Claims,
                claimInformation?.ClientId);

            var subjectId = _claimsService.GetEffectiveSubjectId(claimInformation, user);

            // this allows us to collect any additonal claims or properties
            // for the specific prtotocols used and store them in the local auth cookie.
            // this is typically used to store data needed for signout from those protocols.
            //var additionalLocalClaims = new List<Claim>();
            //var localSignInProps = new AuthenticationProperties();
            //ProcessLoginCallbackForOidc(result, additionalLocalClaims, localSignInProps);
            //ProcessLoginCallbackForWsFed(result, additionalLocalClaims, localSignInProps);
            //ProcessLoginCallbackForSaml2p(result, additionalLocalClaims, localSignInProps);

            var successfulEvent = new FabricUserLoginSuccessEvent(
                claimInformation.Provider,
                claimInformation.UserId,
                subjectId,
                user.Username,
                claimInformation.ClientId);

            // issue authentication cookie for user
            await _events.RaiseAsync(successfulEvent);
            await HttpContext.SignInAsync(user.SubjectId,
                user.Username,
                claimInformation.Provider,
                claimInformation.AuthenticationProperties,
                claimInformation.AdditionalClaims);

            // delete temporary cookie used during external authentication
            await HttpContext.SignOutAsync(IdentityServer4.IdentityServerConstants.ExternalCookieAuthenticationScheme);

           
            if (context != null)
            {
                if (await _clientStore.IsPkceClientAsync(context.ClientId))
                {
                    // if the client is PKCE then we assume it's native, so this change in how to
                    // return the response is for better UX for the end user.
                    return View("Redirect", new RedirectViewModel { RedirectUrl = returnUrl });
                }
            }

            return Redirect(returnUrl);
        }

        private async Task<IActionResult> ProcessWindowsLoginAsync(string returnUrl)
        {
            // see if windows auth has already been requested and succeeded
            var result = await HttpContext.AuthenticateAsync(AccountOptions.WindowsAuthenticationSchemeName);
            if (result?.Principal is WindowsPrincipal wp)
            {
                // we will issue the external cookie and then redirect the
                // user back to the external callback, in essence, treating windows
                // auth the same as any other external authentication mechanism
                var props = new AuthenticationProperties()
                {
                    RedirectUri = Url.Action("Callback"),
                    Items =
                    {
                        { "returnUrl", returnUrl },
                        { "scheme", AccountOptions.WindowsAuthenticationSchemeName },
                    }
                };

                var id = new ClaimsIdentity(AccountOptions.WindowsAuthenticationSchemeName);
                id.AddClaim(new Claim(JwtClaimTypes.Subject, wp.Identity.Name));
                id.AddClaim(new Claim(JwtClaimTypes.Name, wp.Identity.Name));

                var externalUser = await _externalIdentityProviderService.FindUserBySubjectId(HttpContext.User.Identity.Name);
                if (externalUser?.FirstName != null)
                {
                    id.AddClaim(new Claim(JwtClaimTypes.GivenName, externalUser.FirstName));
                }

                if (externalUser?.LastName != null)
                {
                    id.AddClaim(new Claim(JwtClaimTypes.FamilyName, externalUser.LastName));
                }

                // add the groups as claims -- be careful if the number of groups is too large
                if (AccountOptions.IncludeWindowsGroups)
                {
                    var wi = wp.Identity as WindowsIdentity;
                    var groups = wi.Groups.Translate(typeof(NTAccount));
                    var roles = groups.Select(x => new Claim(JwtClaimTypes.Role, x.Value));
                    id.AddClaims(_groupFilterService.FilterClaims(roles));
                }

                await HttpContext.SignInAsync(
                    IdentityServer4.IdentityServerConstants.ExternalCookieAuthenticationScheme,
                    new ClaimsPrincipal(id),
                    props);
                return Redirect(props.RedirectUri);
            }
            else
            {
                // trigger windows auth
                // since windows auth don't support the redirect uri,
                // this URL is re-triggered when we call challenge
                return Challenge(AccountOptions.WindowsAuthenticationSchemeName);
            }
        }

        private (TestUser user, string provider, string providerUserId, IEnumerable<Claim> claims) FindUserFromExternalProvider(AuthenticateResult result)
        {
            var externalUser = result.Principal;

            // try to determine the unique id of the external user (issued by the provider)
            // the most common claim type for that are the sub claim and the NameIdentifier
            // depending on the external provider, some other claim type might be used
            var userIdClaim = externalUser.FindFirst(JwtClaimTypes.Subject) ??
                              externalUser.FindFirst(ClaimTypes.NameIdentifier) ??
                              throw new Exception("Unknown userid");

            // remove the user id claim so we don't include it as an extra claim if/when we provision the user
            var claims = externalUser.Claims.ToList();
            claims.Remove(userIdClaim);

            var provider = result.Properties.Items["scheme"];
            var providerUserId = userIdClaim.Value;

            // find external user
            var user = _users.FindByExternalProvider(provider, providerUserId);

            return (user, provider, providerUserId, claims);
        }

        private TestUser AutoProvisionUser(string provider, string providerUserId, IEnumerable<Claim> claims)
        {
            var user = _users.AutoProvisionUser(provider, providerUserId, claims.ToList());
            return user;
        }

        private void ProcessLoginCallbackForOidc(AuthenticateResult externalResult, List<Claim> localClaims, AuthenticationProperties localSignInProps)
        {
            // if the external system sent a session id claim, copy it over
            // so we can use it for single sign-out
            var sid = externalResult.Principal.Claims.FirstOrDefault(x => x.Type == JwtClaimTypes.SessionId);
            if (sid != null)
            {
                localClaims.Add(new Claim(JwtClaimTypes.SessionId, sid.Value));
            }

            // if the external provider issued an id_token, we'll keep it for signout
            var id_token = externalResult.Properties.GetTokenValue("id_token");
            if (id_token != null)
            {
                localSignInProps.StoreTokens(new[] { new AuthenticationToken { Name = "id_token", Value = id_token } });
            }
        }

        private void ProcessLoginCallbackForWsFed(AuthenticateResult externalResult, List<Claim> localClaims, AuthenticationProperties localSignInProps)
        {
        }

        private void ProcessLoginCallbackForSaml2p(AuthenticateResult externalResult, List<Claim> localClaims, AuthenticationProperties localSignInProps)
        {
        }

        private ObjectResult LogAndReturnStatus(int statusCode, string logMessage, string userMessage = null)
        {
            var returnMessage = userMessage ?? logMessage;

            _logger.Error(logMessage);
            return this.StatusCode(statusCode, userMessage);
        }

        private TestUserStore MakeTestUserStore(IAppConfiguration appConfiguration)
        {
            if (appConfiguration.HostingOptions != null && appConfiguration.HostingOptions.UseTestUsers)
            {
                return new TestUserStore(TestUsers.Users);
            }
            return new TestUserStore(new List<TestUser>());
        }
    }
}