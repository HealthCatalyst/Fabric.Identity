// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.


using System.Collections.Generic;
using IdentityServer4.Services;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using Fabric.Identity.API.Configuration;
using Fabric.Identity.API.Models;
using Fabric.Identity.API.Persistence;
using IdentityModel.Client;

namespace IdentityServer4.Quickstart.UI
{
    [SecurityHeaders]
    public class HomeController : Controller
    {
        private readonly IIdentityServerInteractionService _interaction;
        private readonly IAppConfiguration _appConfiguration;
        private readonly IClientManagementStore _clientManagementStore;

        public HomeController(IIdentityServerInteractionService interaction, IAppConfiguration appConfiguration, IClientManagementStore clientStore)
        {
            _interaction = interaction;
            _appConfiguration = appConfiguration;
            _clientManagementStore = clientStore;
        }

        public async Task<IActionResult> Index()
        {
            var model = new IdentityStatusModel();
            var discoPolicy = new DiscoveryPolicy { ValidateIssuerName = false, RequireHttps = false };
            using (var discoClient =
                new DiscoveryClient(_appConfiguration.IdentityServerConfidentialClientSettings.Authority)
                {
                    Policy = discoPolicy
                })
            {
                var discoveryDocument = await discoClient.GetAsync();
                model.ScopesSupported = discoveryDocument?.ScopesSupported ?? new List<string>();
                model.GrantsSupported = discoveryDocument?.GrantTypesSupported ?? new List<string>();
            }
            model.ClientCount = _clientManagementStore.GetClientCount();

            return View(model);
        }

        /// <summary>
        /// Shows the error page
        /// </summary>
        public async Task<IActionResult> Error(string errorId)
        {
            var vm = new ErrorViewModel();

            // retrieve error details from identityserver
            var message = await _interaction.GetErrorContextAsync(errorId);
            if (message != null)
            {
                vm.Error = message;
            }

            return View("Error", vm);
        }
    }
}