// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.


using IdentityModel;
using IdentityServer4.Services;
using IdentityServer4.Stores;
using IdentityServer4.Test;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Authentication;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Security.Principal;
using System.Threading.Tasks;
using Fabric.Identity.API.Configuration;
using Fabric.Identity.API.DocumentDbStores;
using Fabric.Identity.API.Models;
using Fabric.Identity.API.Services;
using Microsoft.AspNetCore.Authentication;
using IdentityServer4.Events;
using IdentityServer4.Extensions;
using Serilog;

namespace IdentityServer4.Quickstart.UI
{
    /// <summary>
    /// This sample controller implements a typical login/logout/provision workflow for local and external accounts.
    /// The login service encapsulates the interactions with the user data store. This data store is in-memory only and cannot be used for production!
    /// The interaction service provides a way for the UI to communicate with identityserver for validation and context retrieval
    /// </summary>
    [SecurityHeaders]
    public class AccountController : Controller
    {
        private readonly TestUserStore _users;
        private readonly IIdentityServerInteractionService _interaction;
        private readonly IEventService _events;
        private readonly IAppConfiguration _appConfiguration;
        private readonly ILogger _logger;
        private readonly AccountService _account;
        private readonly UserFunctions _userFunctions;

        public AccountController(
            IIdentityServerInteractionService interaction,
            IClientStore clientStore,
            IHttpContextAccessor httpContextAccessor,
            IEventService events,
            IAppConfiguration appConfiguration,
            DocumentDbUserStore documentDbUserStore,
            ILogger logger,
            TestUserStore users = null)
        {
            // if the TestUserStore is not in DI, then we'll just use the global users collection
            _users = users ?? MakeTestUserStore(appConfiguration);
            _interaction = interaction;
            _events = events;
            _appConfiguration = appConfiguration;
            _logger = logger;
            _account = new AccountService(interaction, httpContextAccessor, clientStore, appConfiguration);
            _userFunctions = new UserFunctions(_users, documentDbUserStore);
            
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
        public async Task<IActionResult> Login(string returnUrl)
        {
            var vm = await _account.BuildLoginViewModelAsync(returnUrl);

            if (vm.IsExternalLoginOnly)
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
                    await _events.RaiseAsync(new UserLoginSuccessEvent(user.Username, user.SubjectId, user.Username));
                    await HttpContext.Authentication.SignInAsync(user.SubjectId, user.Username, props);

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
            var vm = await _account.BuildLoginViewModelAsync(model);
            return View(vm);
        }

        /// <summary>
        /// initiate roundtrip to external authentication provider
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> ExternalLogin(string provider, string returnUrl)
        {
            returnUrl = Url.Action("ExternalLoginCallback", new { returnUrl = returnUrl });

            _logger.Information("windows authentication is modeled as external in the asp.net core authentication manager, so we need special handling");
            if (AccountOptions.WindowsAuthenticationSchemes.Contains(provider))
            {
                _logger.Information("but they don't support the redirect uri, so this URL is re-triggered when we call challenge"); 
                if (HttpContext.User is WindowsPrincipal wp)
                {
                    var props = new AuthenticationProperties();
                    props.Items.Add("scheme", AccountOptions.WindowsAuthenticationProviderName);

                    var id = new ClaimsIdentity(provider);
                    id.AddClaim(new Claim(JwtClaimTypes.Subject, HttpContext.User.Identity.Name));
                    id.AddClaim(new Claim(JwtClaimTypes.Name, HttpContext.User.Identity.Name));

                    _logger.Information("add the groups as claims -- be careful if the number of groups is too large");
                    if (AccountOptions.IncludeWindowsGroups)
                    {
                        var wi = wp.Identity as WindowsIdentity;
                        var groups = wi.Groups.Translate(typeof(NTAccount));
                        var roles = groups.Select(x => new Claim(JwtClaimTypes.Role, x.Value));
                        id.AddClaims(roles);
                    }

                    await HttpContext.Authentication.SignInAsync(IdentityServerConstants.ExternalCookieAuthenticationScheme, new ClaimsPrincipal(id), props);
                    return Redirect(returnUrl);
                }
                else
                {
                    _logger.Information("this triggers all of the windows auth schemes we're supporting so the browser can use what it supports");
                    return new ChallengeResult(AccountOptions.WindowsAuthenticationSchemes);
                }
            }
            else
            {
                _logger.Information("start challenge and roundtrip the return URL");
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
            _logger.Information("read external identity from the temporary cookie");
            var info = await HttpContext.Authentication.GetAuthenticateInfoAsync(IdentityServerConstants.ExternalCookieAuthenticationScheme);
            var tempUser = info?.Principal;
            if (tempUser == null)
            {
                throw new Exception("External authentication error");
            }

            _logger.Information("retrieve claims of the external user");
            var claims = tempUser.Claims.ToList();

            _logger.Information("try to determine the unique id of the external user - the most common claim type for that are the sub claim and the NameIdentifier");
            _logger.Information("depending on the external provider, some other claim type might be used");
            var userIdClaim = claims.FirstOrDefault(x => x.Type == JwtClaimTypes.Subject);
            if (userIdClaim == null)
            {
                userIdClaim = claims.FirstOrDefault(x => x.Type == ClaimTypes.NameIdentifier);
            }
            if (userIdClaim == null)
            {
                throw new Exception("Unknown userid");
            }

            _logger.Information("remove the user id claim from the claims collection and move to the userId property");
            _logger.Information("also set the name of the external authentication provider");
            claims.Remove(userIdClaim);
            var provider = info.Properties.Items["scheme"];
            var userId = userIdClaim.Value;

            UserInfo userInfo;
            if (_appConfiguration.HostingOptions.UseTestUsers)
            {
                _logger.Information("check if the external user is already provisioned");
                var user = _userFunctions.FindTestUserByExternalProvider(provider, userId);
                if (user == null)
                {
                    _logger.Information("this sample simply auto-provisions new external user");
                    _logger.Information("another common approach is to start a registrations workflow first");
                    user = _userFunctions.AddTestUser(provider, userId, claims);
                }
                else
                {
                    _logger.Information("update the role claims from the provider");
                    _userFunctions.UpdateTestUserRoleClaims(user, claims);
                }
                userInfo = new UserInfo(user);
            }
            else
            {
                _logger.Information("check if the external user is already provisioned");
                var user = await _userFunctions.FindUserByExternalProvider(provider, userId);
                if (user == null)
                {
                    _logger.Information("this sample simply auto-provisions new external user");
                    _logger.Information("another common approach is to start a registrations workflow first");
                    user =  _userFunctions.AddUser(provider, userId, claims);                   
                }
                else
                {
                    _logger.Information("update the role claims from the provider");
                    _userFunctions.UpdateUserRoleClaims(user, claims);
                }
                userInfo = new UserInfo(user);
                _logger.Information("update the user model with the login");
                var context = await _interaction.GetAuthorizationContextAsync(returnUrl);
                _logger.Information($"authorization context: {context?.ClientId} user subjectId: {userInfo.SubjectId}");
                await _userFunctions.SetLastLogin(context?.ClientId, userInfo.SubjectId);
            }

            var additionalClaims = new List<Claim>();

            _logger.Information("if the external system sent a session id claim, copy it over");
            var sid = claims.FirstOrDefault(x => x.Type == JwtClaimTypes.SessionId);
            if (sid != null)
            {
                additionalClaims.Add(new Claim(JwtClaimTypes.SessionId, sid.Value));
            }

            _logger.Information("if the external provider issues groups claims, copy it over");
            var groupClaims = claims.Where(c => c.Type == "groups").ToList();
            if (groupClaims.Any())
            {
                additionalClaims.AddRange(groupClaims);
            }

            _logger.Information("if the external provider issued an id_token, we'll keep it for signout");
            AuthenticationProperties props = null;
            var id_token = info.Properties.GetTokenValue("id_token");
            if (id_token != null)
            {
                props = new AuthenticationProperties();
                props.StoreTokens(new[] { new AuthenticationToken { Name = "id_token", Value = id_token } });
            }

            _logger.Information("issue authentication cookie for user");
            await _events.RaiseAsync(new UserLoginSuccessEvent(provider, userId, userInfo.SubjectId, userInfo.Username));
            await HttpContext.Authentication.SignInAsync(userInfo.SubjectId, userInfo.Username, provider, props, additionalClaims.ToArray());

            _logger.Information("delete temporary cookie used during external authentication");
            await HttpContext.Authentication.SignOutAsync(IdentityServerConstants.ExternalCookieAuthenticationScheme);

            _logger.Information("validate return URL and redirect back to authorization endpoint or a local page");
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
            var vm = await _account.BuildLogoutViewModelAsync(logoutId);

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
            var vm = await _account.BuildLoggedOutViewModelAsync(model.LogoutId);
            if (vm.TriggerExternalSignout)
            {
                string url = Url.Action("Logout", new { logoutId = vm.LogoutId });
                try
                {
                    // hack: try/catch to handle social providers that throw
                    await HttpContext.Authentication.SignOutAsync(vm.ExternalAuthenticationScheme,
                        new AuthenticationProperties { RedirectUri = url });
                }
                catch (NotSupportedException) // this is for the external providers that don't have signout
                {
                }
                catch (InvalidOperationException) // this is for Windows/Negotiate
                {
                }
            }

            // delete local authentication cookie
            await HttpContext.Authentication.SignOutAsync();

            var user = await HttpContext.GetIdentityServerUserAsync();
            if (user != null)
            {
                await _events.RaiseAsync(new UserLogoutSuccessEvent(user.GetSubjectId(), user.GetName()));
            }

            return View("LoggedOut", vm);
        }        
    }

    public class UserInfo
    {
        public string SubjectId { get; set; }
        public string Username { get; set; }

        public UserInfo(TestUser user)
        {
            SubjectId = user.SubjectId;
            Username = user.Username;
        }

        public UserInfo(User user)
        {
            SubjectId = user.SubjectId;
            Username = user.Username;
        }
    }
}