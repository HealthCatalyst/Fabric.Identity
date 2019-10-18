using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fabric.Identity.API.Models;
using Fabric.Identity.API.Validation;
using Fabric.IdentityProviderSearchService.Validators;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Serilog;

namespace Fabric.Identity.API.Management
{
    /// <summary>
    /// Find Users or Groups either registered in Identity or from AD or AAD.
    /// </summary>
    [Authorize(Policy = FabricIdentityConstants.AuthorizationPolicyNames.SearchUsersScopeClaim, AuthenticationSchemes = "Bearer")]
    [ApiVersion("1.0")]
    [Route("api/principals")]
    [Route("api/v{version:apiVersion}/principals")]
    public class PrincipalsController : BaseController<PrincipalSearchRequest>
    {
        public PrincipalsController(SearchRequestValidator validator, ILogger logger) : base(validator, logger)
        {
        }

        public async Task<IActionResult> Search(PrincipalSearchRequest searchRequest)
        {
            return await ValidateAndExecuteAsync(searchRequest, async () =>
            {
                return Ok();
            });
        }
    }
}
