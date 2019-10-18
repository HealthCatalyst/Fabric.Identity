using System.Net;
using Fabric.Identity.API.Models;
using Fabric.Identity.API.Persistence;
using Fabric.Identity.API.Validation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Serilog;
using Swashbuckle.AspNetCore.Annotations;
using IS4 = IdentityServer4.Models;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace Fabric.Identity.API.Management
{
    /// <summary>
    /// </summary>
    [Authorize(Policy = FabricIdentityConstants.AuthorizationPolicyNames.RegistrationThreshold,
        AuthenticationSchemes = "Bearer")]
    [ApiVersion("1.0")]
    [Route("api/identityresource")]
    [Route("api/v{version:apiVersion}/identityresource")]
    public class IdentityResourceController : BaseController<IS4.IdentityResource>
    {
        private const string NotFoundErrorMsg = "The specified Identity resource id could not be found.";

        private readonly IIdentityResourceStore _identityResourceStore;

        /// <summary>
        ///     Constructor
        /// </summary>
        /// <param name="identityResourceStore">IIdentityResourceStore</param>
        /// <param name="validator">IdentityResourceValidator</param>
        /// <param name="logger">ILogger</param>
        public IdentityResourceController(IIdentityResourceStore identityResourceStore,
            IdentityResourceValidator validator, ILogger logger)
            : base(validator, logger)
        {
            _identityResourceStore = identityResourceStore;
        }

        /// <summary>
        ///     Retrieve Identity resource by <paramref name="id" />.
        /// </summary>
        /// <param name="id">The unique identifier of the identity resource</param>
        /// <returns></returns>
        [HttpGet("{id}")]
        [SwaggerResponse(404, NotFoundErrorMsg, typeof(Error))]
        [SwaggerResponse(400, BadRequestErrorMsg, typeof(Error))]
        public IActionResult Get(string id)
        {
            var is4IdentityResource = _identityResourceStore.GetResource(id);

            if (is4IdentityResource == null)
            {
                return CreateFailureResponse($"The specified identity resource with id: {id} was not found",
                    HttpStatusCode.NotFound);
            }
            return Ok(is4IdentityResource.ToIdentityResourceViewModel());
        }

        /// <summary>
        ///     Creates an Identity resource.
        /// </summary>
        /// <param name="identityResource">The <see cref="IdentityResource" />IdentityResource to add.</param>
        /// <returns></returns>
        [HttpPost]
        [SwaggerResponse(201, "The identity resource was created.", typeof(IdentityResource))]
        [SwaggerResponse(400, BadRequestErrorMsg, typeof(Error))]
        [SwaggerResponse(409, DuplicateErrorMsg, typeof(Error))]
        public IActionResult Post([FromBody] IdentityResource identityResource)
        {
            var is4IdentityResource = identityResource.ToIs4IdentityResource();
            return ValidateAndExecute(is4IdentityResource, () =>
            {
                var existingResource = _identityResourceStore.GetResource(identityResource.Name);
                if (existingResource != null)
                {
                    return CreateFailureResponse(
                        $"Identity resource {identityResource.Name} already exists. Please provide a new name",
                        HttpStatusCode.Conflict);
                }

                _identityResourceStore.AddResource(is4IdentityResource);
                return CreatedAtAction("Get", new {id = identityResource.Name},
                    is4IdentityResource.ToIdentityResourceViewModel());
            });
        }

        /// <summary>
        ///     Modifies the Identity resource by <paramref name="id" />.
        /// </summary>
        /// <param name="id">The unique identifier of the identity resource</param>
        /// <param name="identityResource">The <see cref="IdentityResource" /> IdenetityResource to update.</param>
        /// <returns></returns>
        [HttpPut("{id}")]
        [SwaggerResponse(204, "The identity resource was updated.")]
        [SwaggerResponse(404, NotFoundErrorMsg, typeof(Error))]
        [SwaggerResponse(400, BadRequestErrorMsg, typeof(Error))]
        [SwaggerResponse(409, DuplicateErrorMsg, typeof(Error))]
        public IActionResult Put(string id, [FromBody] IdentityResource identityResource)
        {
            var is4IdentityResource = identityResource.ToIs4IdentityResource();
            return ValidateAndExecute(is4IdentityResource, () =>
            {
                var storedIdentityResource = _identityResourceStore.GetResource(id);
                if (string.IsNullOrEmpty(storedIdentityResource?.Name))
                {
                    return CreateFailureResponse($"The specified Identity resource with id:{id} was not found",
                        HttpStatusCode.NotFound);
                }
                _identityResourceStore.UpdateResource(id, is4IdentityResource);
                return NoContent();
            });
        }

        /// <summary>
        ///     Deletes the Identity resource by <paramref name="id" />.
        /// </summary>
        /// <param name="id">The unique identifier of the identity resource.</param>
        /// <returns></returns>
        [HttpDelete("{id}")]
        [SwaggerResponse(204, "The specified identity resource was deleted.")]
        [SwaggerResponse(404, NotFoundErrorMsg, typeof(Error))]
        public IActionResult Delete(string id)
        {
            var is4IdentityResource = _identityResourceStore.GetResource(id);
            if (is4IdentityResource == null)
            {
                return CreateFailureResponse($"The specified identity resource with id: {id} was not found",
                    HttpStatusCode.NotFound);
            }

            _identityResourceStore.DeleteResource(id);
            return NoContent();
        }
    }
}