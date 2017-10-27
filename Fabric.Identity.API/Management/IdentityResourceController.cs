using System.Net;
using Fabric.Identity.API.Models;
using Fabric.Identity.API.Services;
using Fabric.Identity.API.Validation;
using IdentityServer4.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Serilog;
using Swashbuckle.AspNetCore.SwaggerGen;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace Fabric.Identity.API.Management
{
    /// <summary>
    /// 
    /// </summary>
    [Authorize(Policy = FabricIdentityConstants.AuthorizationPolicyNames.RegistrationThreshold, ActiveAuthenticationSchemes = "Bearer")]
    [ApiVersion("1.0")]
    [Route("api/identityresource")]
    [Route("api/v{version:apiVersion}/identityresource")]
    public class IdentityResourceController : BaseController<IdentityResource>
    {
        private const string NotFoundErrorMsg = "The specified Identity resource id could not be found.";

        private readonly IDocumentDbService _documentDbService;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="documentDbService">IDocumentDbService</param>
        /// <param name="validator">IdentityResourceValidator</param>
        /// <param name="logger">ILogger</param>
        public IdentityResourceController(IDocumentDbService documentDbService, IdentityResourceValidator validator, ILogger logger) 
            : base(validator, logger)
        {
            _documentDbService = documentDbService;
        }

        /// <summary>
        /// Retrieve Identity resource by <paramref name="id"/>.
        /// </summary>
        /// <param name="id">The unique identifier of the identity resource</param>
        /// <returns></returns>
        [HttpGet("{id}")]
        [SwaggerResponse(404, typeof(Error), NotFoundErrorMsg)]
        [SwaggerResponse(400, typeof(Error), BadRequestErrorMsg)]
        public IActionResult Get(string id)
        {
            var identityResource = _documentDbService.GetDocument<IdentityResource>(id).Result;

            if (identityResource == null)
            {
                return CreateFailureResponse($"The specified client with id: {id} was not found",
                    HttpStatusCode.NotFound);
            }
            return Ok(identityResource);
        }

        /// <summary>
        /// Creates an Identity resource.
        /// </summary>
        /// <param name="value">The <see cref="IdentityResource"/> object to add.</param>
        /// <returns></returns>
        [HttpPost]
        [SwaggerResponse(201, typeof(IdentityResource), "The identity resource was created.")]
        [SwaggerResponse(400, typeof(Error), BadRequestErrorMsg)]
        public IActionResult Post([FromBody] IdentityResource value)
        {
            return ValidateAndExecute(value, () =>
            {
                var id = value.Name;
                _documentDbService.AddDocument(id, value);
                return CreatedAtAction("Get", new { id }, value);
            });
        }

        /// <summary>
        /// Modifies the Identity resource by <paramref name="id"/>.
        /// </summary>
        /// <param name="id">The unique identifier of the identity resource</param>
        /// <param name="value">The <see cref="IdentityResource"/> object to update.</param>
        /// <returns></returns>
        [HttpPut("{id}")]
        [SwaggerResponse(204, null, "The identity resource was updated.")]
        [SwaggerResponse(404, typeof(Error), NotFoundErrorMsg)]
        [SwaggerResponse(400, typeof(Error), BadRequestErrorMsg)]
        public IActionResult Put(string id, [FromBody] IdentityResource value)
        {
            return ValidateAndExecute(value, () =>
            {
                _documentDbService.UpdateDocument(id, value);
                return NoContent();
            });
        }

        /// <summary>
        /// Deletes the Identity resource by <paramref name="id"/>.
        /// </summary>
        /// <param name="id">The unique identifier of the identity resource.</param>
        /// <returns></returns>
        [HttpDelete("{id}")]
        [SwaggerResponse(204, null, "The specified identity resource was deleted.")]
        [SwaggerResponse(404, typeof(Error), NotFoundErrorMsg)]
        public IActionResult Delete(string id)
        {
            Get(id);

            _documentDbService.DeleteDocument<IdentityResource>(id);

            return NoContent();
        }
    }
}
