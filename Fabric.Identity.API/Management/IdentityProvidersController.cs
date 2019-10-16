using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Fabric.Identity.API.Models;
using Fabric.Identity.API.Services;
using Fabric.Identity.API.Validation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Serilog;
using Swashbuckle.AspNetCore.Annotations;

namespace Fabric.Identity.API.Management
{
    /// <summary>
    /// Manage third party identity providers configured for authentication
    /// </summary>
    [Authorize(Policy = FabricIdentityConstants.AuthorizationPolicyNames.ReadScopeClaim, AuthenticationSchemes = "Bearer")]
    [ApiVersion("1.0")]
    [Route("api/identityproviders")]
    [Route("api/v{version:apiVersion}/identityproviders")]
    public class IdentityProvidersController : BaseController<ExternalProviderApiModel>
    {
        private readonly IIdentityProviderConfigurationService _identityProviderConfigurationService;
        public IdentityProvidersController(IIdentityProviderConfigurationService identityProviderConfigurationService, 
            ExternalProviderApiModelValidator validator, 
            ILogger logger) : base(validator, logger)
        {
            _identityProviderConfigurationService = identityProviderConfigurationService ??
                                                    throw new ArgumentNullException(
                                                        nameof(identityProviderConfigurationService));
        }

        /// <summary>
        /// Get a list of all the configured third party identity providers.
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [SwaggerResponse((int)HttpStatusCode.OK, "Success", typeof(List<ExternalProviderApiModel>))]
        [SwaggerResponse((int)HttpStatusCode.BadRequest, BadRequestErrorMsg, typeof(Error))]
        public async Task<IActionResult> Get()
        {
            var providers = await _identityProviderConfigurationService.GetConfiguredIdentityProviders();
            return Ok(providers.Select(p => new ExternalProviderApiModel
            {
                AuthenticationScheme = p.AuthenticationScheme,
                DisplayName = p.DisplayName
            }));
        }
    }
}
