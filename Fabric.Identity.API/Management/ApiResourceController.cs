using System.Collections.Generic;
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
    ///     Manage metadata for APIs protected by the Identity API.
    /// </summary>
    [Authorize(Policy = FabricIdentityConstants.AuthorizationPolicyNames.RegistrationThreshold,
        AuthenticationSchemes = "Bearer")]
    [ApiVersion("1.0")]
    [Route("api/apiresource")]
    [Route("api/v{version:apiVersion}/apiresource")]
    public class ApiResourceController : BaseController<IS4.ApiResource>
    {
        private const string NotFoundErrorMsg = "The specified API resource id could not be found.";

        private readonly IApiResourceStore _apiResourceStore;

        /// <summary>
        ///     Constructor
        /// </summary>
        /// <param name="apiResourceStore">IApiResourceStore</param>
        /// <param name="validator">ApiResourceValidator</param>
        /// <param name="logger">ILogger</param>
        public ApiResourceController(IApiResourceStore apiResourceStore, ApiResourceValidator validator, ILogger logger)
            : base(validator, logger)
        {
            _apiResourceStore = apiResourceStore;
        }

        /// <summary>
        ///     Retrieve API resource by <paramref name="id" />.
        /// </summary>
        /// <param name="id">The unique identifier of the API resource.</param>
        /// <returns>
        ///     <see cref="IS4.ApiResource" />
        /// </returns>
        [HttpGet("{id}")]
        [SwaggerResponse(200, "Success", typeof(ApiResource))]
        [SwaggerResponse(404, NotFoundErrorMsg, typeof(Error))]
        [SwaggerResponse(400, BadRequestErrorMsg, typeof(Error))]
        public IActionResult Get(string id)
        {
            var is4ApiResource = _apiResourceStore.GetResource(id);

            if (is4ApiResource == null)
            {
                return CreateFailureResponse($"The specified api resource with id: {id} was not found",
                    HttpStatusCode.NotFound);
            }

            return Ok(is4ApiResource.ToApiResourceViewModel());
        }

        /// <summary>
        ///     Reset the api secret by API resource <paramref name="id" />.
        /// </summary>
        /// <param name="id">The unique identifier of the API resource.</param>
        /// <returns></returns>
        [HttpPost("{id}/resetPassword")]
        [SwaggerResponse(200, "The password was reset.", typeof(ApiResource))]
        [SwaggerResponse(404, NotFoundErrorMsg, typeof(Error))]
        [SwaggerResponse(400, BadRequestErrorMsg, typeof(Error))]
        public IActionResult ResetPassword(string id)
        {
            var is4ApiResource = _apiResourceStore.GetResource(id);

            if (string.IsNullOrEmpty(is4ApiResource?.Name))
            {
                return CreateFailureResponse($"The specified api resource with id: {id} was not found",
                    HttpStatusCode.NotFound);
            }

            // update password
            var resourceSecret = GeneratePassword();
            is4ApiResource.ApiSecrets = new List<IS4.Secret> {GetNewSecret(resourceSecret)};
            _apiResourceStore.UpdateResource(id, is4ApiResource);

            var viewApiResource = is4ApiResource.ToApiResourceViewModel();
            viewApiResource.ApiSecret = resourceSecret;
            return Ok(viewApiResource);
        }

        /// <summary>
        ///     Creates an API resource.
        /// </summary>
        /// <param name="apiResource">The <see cref="ApiResource" /> object to add.</param>
        /// <returns></returns>
        [HttpPost]
        [SwaggerResponse(201, "The API resource was created.", typeof(ApiResource))]
        [SwaggerResponse(400, BadRequestErrorMsg, typeof(Error))]
        [SwaggerResponse(409, DuplicateErrorMsg, typeof(Error))]
        public IActionResult Post([FromBody] ApiResource apiResource)
        {
            var is4ApiResource = apiResource.ToIs4ApiResource();
            return ValidateAndExecute(is4ApiResource, () =>
            {
                var existingResource = _apiResourceStore.GetResource(apiResource.Name);
                if (existingResource != null)
                {
                    return CreateFailureResponse(
                        $"Api resource {apiResource.Name} already exists. Please provide a new name",
                        HttpStatusCode.Conflict);
                }

                // override any secret in the request.
                // TODO: we need to implement a salt strategy, either at the controller level or store level.
                var resourceSecret = GeneratePassword();
                is4ApiResource.ApiSecrets = new List<IS4.Secret> {GetNewSecret(resourceSecret)};
                _apiResourceStore.AddResource(is4ApiResource);

                var viewResource = is4ApiResource.ToApiResourceViewModel();
                viewResource.ApiSecret = resourceSecret;
                return CreatedAtAction("Get", new { id = apiResource.Name }, viewResource);
            }, $"{FabricIdentityConstants.ValidationRuleSets.ApiResourcePost},{FabricIdentityConstants.ValidationRuleSets.Default}");
        }

        /// <summary>
        ///     Modifies the API resource by <paramref name="id" />.
        /// </summary>
        /// <param name="id">The unique identifier of the API resource.</param>
        /// <param name="apiResource">The <see cref="ApiResource" /> object to update.</param>
        /// <returns></returns>
        [HttpPut("{id}")]
        [SwaggerResponse(204, "No Content")]
        [SwaggerResponse(404, NotFoundErrorMsg, typeof(Error))]
        [SwaggerResponse(400, BadRequestErrorMsg, typeof(Error))]
        [SwaggerResponse(409, DuplicateErrorMsg, typeof(Error))]
        public IActionResult Put(string id, [FromBody] ApiResource apiResource)
        {
            var is4ApiResource = apiResource.ToIs4ApiResource();
            return ValidateAndExecute(is4ApiResource, () =>
            {
                if (!string.Equals(id, apiResource.Name))
                {
                    return CreateFailureResponse(
                        "The ApiResource Name in the request URL path must match the ApiResource Name in the request body.", HttpStatusCode.BadRequest);
                }

                var storedApiResource = _apiResourceStore.GetResource(id);

                if (string.IsNullOrEmpty(storedApiResource?.Name))
                {
                    return CreateFailureResponse($"The specified api resource with id:{id} was not found",
                        HttpStatusCode.NotFound);
                }

                // Prevent from changing secrets.
                is4ApiResource.ApiSecrets = storedApiResource.ApiSecrets;
                // Prevent from changing payload Name.
                is4ApiResource.Name = id;

                _apiResourceStore.UpdateResource(id, is4ApiResource);
                return NoContent();
            });
        }

        /// <summary>
        ///     Deletes the API resource by <paramref name="id" />.
        /// </summary>
        /// <param name="id">The unique identifier of the API resource.</param>
        /// <returns></returns>
        [HttpDelete("{id}")]
        [SwaggerResponse(204, "The specified API resource was deleted.")]
        [SwaggerResponse(404, NotFoundErrorMsg, typeof(Error))]
        public IActionResult Delete(string id)
        {
            var is4ApiResource = _apiResourceStore.GetResource(id);
            if (is4ApiResource == null)
            {
                return CreateFailureResponse($"The specified api resource with id: {id} was not found",
                    HttpStatusCode.NotFound);
            }

            _apiResourceStore.DeleteResource(id);
            return NoContent();
        }
    }
}