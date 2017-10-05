using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Fabric.Identity.API.Models;
using Fabric.Identity.API.Services;
using Fabric.Identity.API.Validation;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Serilog;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Fabric.Identity.API.Management
{
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

        [HttpGet]
        [SwaggerResponse((int)HttpStatusCode.OK, typeof(List<ExternalProviderApiModel>), "Success")]
        [SwaggerResponse((int)HttpStatusCode.BadRequest, typeof(Error), BadRequestErrorMsg)]
        public IActionResult Get()
        {
            var providers = _identityProviderConfigurationService.GetConfiguredIdentityProviders();
            return Ok(providers.Select(p => new ExternalProviderApiModel
            {
                AuthenticationScheme = p.AuthenticationScheme,
                DisplayName = p.DisplayName
            }));
        }
    }
}
