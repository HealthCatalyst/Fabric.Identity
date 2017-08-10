using System.Net;
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
    [Authorize(Policy = "RegistrationThreshold", ActiveAuthenticationSchemes = "Bearer")]
    [ApiVersion("1.0")]
    [Route("api/identityresource")]
    [Route("api/v{version:apiVersion}/identityresource")]
    public class IdentityResourceController : BaseController<IdentityResource>
    {
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
        /// <param name="value"></param>
        /// <returns></returns>
        [SwaggerResponse(201, typeof(IdentityResource), "Success")]
        [SwaggerResponse(400, typeof(BadRequestObjectResult), "Bad Request")]
        [HttpPost]
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
        /// <param name="value"></param>
        /// <returns></returns>
        [SwaggerResponse(204, null, "No Content")]
        [SwaggerResponse(404, typeof(NotFoundObjectResult), "Not Found")]
        [SwaggerResponse(400, typeof(BadRequestObjectResult), "Bad Request")]
        [HttpPut("{id}")]
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
        [SwaggerResponse(204, null, "No Content")]
        [HttpDelete("{id}")]
        public IActionResult Delete(string id)
        {
            _documentDbService.DeleteDocument<IdentityResource>(id);

            return NoContent();
        }
    }
}
