// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.


using IdentityModel;
using IdentityServer4.Services;
using IdentityServer4.Stores;
using IdentityServer4.Test;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Security.Principal;
using System.Threading.Tasks;
using Fabric.Identity.API;
using Fabric.Identity.API.Configuration;
using Fabric.Identity.API.Events;
using Fabric.Identity.API.Management;
using Fabric.Identity.API.Persistence;
using Fabric.Identity.API.Services;
using IFabricClaimsService = Fabric.Identity.API.Services.IClaimsService;
using IdentityServer4.Events;
using IdentityServer4.Extensions;
using Serilog;

namespace IdentityServer4.Quickstart.UI
{
    using System.Globalization;

    using Fabric.Identity.API.Exceptions;
    using Fabric.Identity.API.Models;
    using IdentityServer4.Models;

    /// <summary>
    /// This sample controller implements a typical login/logout/provision workflow for local and external accounts.
    /// The login service encapsulates the interactions with the user data store. This data store is in-memory only and cannot be used for production!
    /// The interaction service provides a way for the UI to communicate with identityserver for validation and context retrieval
    /// </summary>
    [TypeFilter(typeof(SecurityHeadersAttribute))]
    public class AccountController : Controller
    {
        private readonly TestUserStore _users;
        private readonly IIdentityServerInteractionService _interaction;
        private readonly IEventService _events;
        private readonly IAppConfiguration _appConfiguration;
        private readonly ILogger _logger;
        private readonly IExternalIdentityProviderService _externalIdentityProviderService;
        private readonly IFabricClaimsService _claimsService;
        private readonly AccountService _accountService;
        private readonly UserLoginManager _userLoginManager;

        private readonly GroupFilterService _groupFilterService;

        private const string TEST_COOKIE_NAME = "testCookie";

        public AccountController(
            IIdentityServerInteractionService interaction,
            IClientStore clientStore,
            IHttpContextAccessor httpContextAccessor,
            IEventService events,
            IAppConfiguration appConfiguration,
            IUserStore userStore,
            ILogger logger,
            IExternalIdentityProviderService externalIdentityProviderService,
            IFabricClaimsService claimsService,
            AccountService accountService,
            GroupFilterService groupFilterService,
            TestUserStore users = null)
        {
            // if the TestUserStore is not in DI, then we'll just use the global users collection
            _users = users ?? MakeTestUserStore(appConfiguration);
            _interaction = interaction;
            _events = events;
            _appConfiguration = appConfiguration;
            _logger = logger;
            _externalIdentityProviderService = externalIdentityProviderService;
            _accountService = accountService;
            _claimsService = claimsService;
            _groupFilterService = groupFilterService;
            _userLoginManager = new UserLoginManager(userStore, _logger);

        }

        private TestUserStore MakeTestUserStore(IAppConfiguration appConfiguration)
        {
            if (appConfiguration.HostingOptions != null && appConfiguration.HostingOptions.UseTestUsers)
            {
                return new TestUserStore(TestUsers.Users);
            }
            return new TestUserStore(new List<TestUser>());
        }

        /// <summary>
        /// Show login page
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Login(string returnUrl, bool testCookieSet = false)
        {
            if (!testCookieSet)
            {
                HttpContext.Response.Cookies.Append(TEST_COOKIE_NAME, "test");

                return RedirectToAction("Login", new {returnUrl, testCookieSet = true});
            }

            var vm = await _accountService.BuildLoginViewModelAsync(returnUrl);

            vm.TestCookieExists = Request.Cookies.ContainsKey(TEST_COOKIE_NAME);

            if (vm.IsExternalLoginOnly && vm.TestCookieExists)
            {
                // only one option for logging in
                return await ExternalLogin(vm.ExternalLoginScheme, returnUrl);
            }

            return View(vm);
        }

        /// <summary>
        /// Handle postback from username/password login
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginInputModel model)
        {
            if (ModelState.IsValid)
            {
                // validate username/password against in-memory store
                if (_users.ValidateCredentials(model.Username, model.Password))
                {
                    AuthenticationProperties props = null;
                    // only set explicit expiration here if persistent. 
                    // otherwise we reply upon expiration configured in cookie middleware.
                    if (AccountOptions.AllowRememberLogin && model.RememberLogin)
                    {
                        props = new AuthenticationProperties
                        {
                            IsPersistent = true,
                            ExpiresUtc = DateTimeOffset.UtcNow.Add(AccountOptions.RememberMeLoginDuration)
                        };
                    };

                    // issue authentication cookie with subject ID and username
                    var user = _users.FindByUsername(model.Username);
                    //get the client id from the auth context
                    var context = await _interaction.GetAuthorizationContextAsync(model.ReturnUrl);
                    await _userLoginManager.UserLogin("test", user.SubjectId, user.Claims.ToList(), context?.ClientId);
                    await _events.RaiseAsync(new FabricUserLoginSuccessEvent("test", user.Username, user.SubjectId, user.Username, context?.ClientId));
                    await HttpContext.SignInAsync(user.SubjectId, user.Username, props);
                    
                    // make sure the returnUrl is still valid, and if yes - redirect back to authorize endpoint or a local page
                    if (_interaction.IsValidReturnUrl(model.ReturnUrl) || Url.IsLocalUrl(model.ReturnUrl))
                    {
                        return Redirect(model.ReturnUrl);
                    }

                    return Redirect("~/");
                }

                await _events.RaiseAsync(new UserLoginFailureEvent(model.Username, "invalid credentials"));

                ModelState.AddModelError("", AccountOptions.InvalidCredentialsErrorMessage);
            }

            // something went wrong, show form with error
            var vm = await _accountService.BuildLoginViewModelAsync(model);
            return View(vm);
        }

        /// <summary>
        /// initiate roundtrip to external authentication provider
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> ExternalLogin(string provider, string returnUrl)
        {
            returnUrl = Url.Action("ExternalLoginCallback", new { returnUrl = returnUrl });

            //windows authentication is modeled as external in the asp.net core authentication manager, so we need special handling
            if (AccountOptions.WindowsAuthenticationSchemes.Contains(provider))
            {
                //"but they don't support the redirect uri, so this URL is re-triggered when we call challenge
                if (HttpContext.User is WindowsPrincipal wp)
                {
                    var props = new AuthenticationProperties();
                    props.Items.Add("scheme", AccountOptions.WindowsAuthenticationProviderName);

                    var id = new ClaimsIdentity(provider);

                    id.AddClaim(new Claim(JwtClaimTypes.Subject, HttpContext.User.Identity.Name));
                    id.AddClaim(new Claim(JwtClaimTypes.Name, HttpContext.User.Identity.Name));
                    id.AddClaim(new Claim(FabricIdentityConstants.PublicClaimTypes.UserPrincipalName, HttpContext.User.Identity.Name));

                    var externalUser = await _externalIdentityProviderService.FindUserBySubjectIdAsync(HttpContext.User.Identity.Name);
                    if (externalUser != null)
                    {
                        if (externalUser.FirstName != null)
                        {
                            id.AddClaim(new Claim(JwtClaimTypes.GivenName, externalUser.FirstName));
                        }

                        if (externalUser.LastName != null)
                        {
                            id.AddClaim(new Claim(JwtClaimTypes.FamilyName, externalUser.LastName));
                        }

                        if (externalUser.Email != null)
                        {
                            id.AddClaim(new Claim(JwtClaimTypes.Email, externalUser.Email));
                        }
                    }

                    //add the groups as claims -- be careful if the number of groups is too large
                    if (AccountOptions.IncludeWindowsGroups)
                    {
                        var wi = wp.Identity as WindowsIdentity;
                        var groups = wi.Groups.Translate(typeof(NTAccount));
                        var roles = groups.Select(x => new Claim(JwtClaimTypes.Role, x.Value));
                        id.AddClaims(_groupFilterService.FilterClaims(roles));
                    }

                    await HttpContext.SignInAsync(IdentityServerConstants.ExternalCookieAuthenticationScheme, new ClaimsPrincipal(id), props);
                    return Redirect(returnUrl);
                }
                else
                {
                    //this triggers all of the windows auth schemes we're supporting so the browser can use what it supports
                    return new ChallengeResult(AccountOptions.WindowsAuthenticationSchemes);
                }
            }
            else
            {
                //start challenge and roundtrip the return URL
                var props = new AuthenticationProperties
                {
                    RedirectUri = returnUrl,
                    Items = { { "scheme", provider } }
                };
                return new ChallengeResult(provider, props);
            }
        }

        /// <summary>
        /// Post processing of external authentication
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> ExternalLoginCallback(string returnUrl)
        {
            //read external identity from the temporary cookie
            var info = await HttpContext.AuthenticateAsync(
                           IdentityServerConstants.ExternalCookieAuthenticationScheme);
            var context = await _interaction.GetAuthorizationContextAsync(returnUrl);

            //retrieve claims of the external user
            ClaimsResult claimInformation = null;

            try
            {
                claimInformation = await _claimsService.GenerateClaimsForIdentity(info, context);
                _logger.Information("Generated claims for Identity: " + claimInformation);
            }
            catch(InvalidIssuerException exc)
            {
                await HttpContext.SignOutAsync(IdentityServerConstants.ExternalCookieAuthenticationScheme);
                return LogAndReturnStatus(403, exc.LogMessage, exc.Message);
            }

            //issue authentication cookie for user
            var user = await _userLoginManager.UserLogin(
                claimInformation.Provider,
                _claimsService.GetEffectiveUserId(claimInformation),
                claimInformation.Claims,
                claimInformation?.ClientId);
            
            var subjectId = _claimsService.GetEffectiveSubjectId(claimInformation, user);

            var successfulEvent = new FabricUserLoginSuccessEvent(
                claimInformation.Provider,
                claimInformation.UserId,
                subjectId,
                user?.Username,
                claimInformation.ClientId);

            await _events.RaiseAsync(successfulEvent);

            await HttpContext.SignInAsync(
                subjectId,
                user?.Username,
                claimInformation.Provider,
                claimInformation.AuthenticationProperties, 
                claimInformation.AdditionalClaims);

            //delete temporary cookie used during external authentication
            await HttpContext.SignOutAsync(IdentityServerConstants.ExternalCookieAuthenticationScheme);
            RemoveTestCookie();

            //validate return URL and redirect back to authorization endpoint or a local page
            if (_interaction.IsValidReturnUrl(returnUrl) || Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }

            return Redirect("~/");
        }

        /// <summary>
        /// Show logout page
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Logout(string logoutId)
        {
            var vm = await _accountService.BuildLogoutViewModelAsync(logoutId);

            if (vm.ShowLogoutPrompt == false)
            {
                // no need to show prompt
                return await Logout(vm);
            }

            return View(vm);
        }

        /// <summary>
        /// Handle logout page postback
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout(LogoutInputModel model)
        {
            var vm = await _accountService.BuildLoggedOutViewModelAsync(model.LogoutId);
            if (vm.TriggerExternalSignout)
            {
                string url = Url.Action("Logout", new { logoutId = vm.LogoutId });
                try
                {
                    // hack: try/catch to handle social providers that throw
                    await HttpContext.SignOutAsync(vm.ExternalAuthenticationScheme,
                        new AuthenticationProperties { RedirectUri = url });
                }
                catch (NotSupportedException) // this is for the external providers that don't have signout
                {
                }
                catch (InvalidOperationException) // this is for Windows/Negotiate
                {
                }
            }

            var user = HttpContext.User;
            // we need to check if the user is authenticated for proper logout,
            // not if user object is null
            if (user?.Identity.IsAuthenticated == true)
            {
                // delete local authentication cookie
                await HttpContext.SignOutAsync();

                await _events.RaiseAsync(new UserLogoutSuccessEvent(user.GetSubjectId(), user.GetDisplayName()));
            }

            return View("LoggedOut", vm);
        }

        private ObjectResult LogAndReturnStatus(int statusCode, string logMessage, string userMessage = null)
        {
            var returnMessage = userMessage ?? logMessage;

            _logger.Error(logMessage);
            return this.StatusCode(statusCode, userMessage);
        }

        private void RemoveTestCookie()
        {
            HttpContext.Response.Cookies.Delete(TEST_COOKIE_NAME);
        }
    }
}