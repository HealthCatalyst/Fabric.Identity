using System.Collections.Generic;
using System.Net;
using Fabric.Identity.API.Models;
using Fabric.Identity.API.Persistence;
using Fabric.Identity.API.Validation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Serilog;
using Swashbuckle.AspNetCore.SwaggerGen;
using IS4 = IdentityServer4.Models;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace Fabric.Identity.API.Management
{
    /// <summary>
    ///     Manage metadata for APIs protected by the Identity API.
    /// </summary>
    [Authorize(Policy = FabricIdentityConstants.AuthorizationPolicyNames.RegistrationThreshold,
        ActiveAuthenticationSchemes = "Bearer")]
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
        /// <param name="documentDbService">IDocumentDbService</param>
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
        [SwaggerResponse(200, typeof(IS4.ApiResource), "Success")]
        [SwaggerResponse(404, typeof(Error), NotFoundErrorMsg)]
        [SwaggerResponse(400, typeof(Error), BadRequestErrorMsg)]
        public IActionResult Get(string id)
        {
            var apiResource = _apiResourceStore.GetResource(id);

            if (apiResource == null)
            {
                return CreateFailureResponse($"The specified api resource with id: {id} was not found",
                    HttpStatusCode.NotFound);
            }

            return Ok(apiResource.ToApiResourceViewModel());
        }

        /// <summary>
        ///     Reset the api secret by API resource <paramref name="id" />.
        /// </summary>
        /// <param name="id">The unique identifier of the API resource.</param>
        /// <returns></returns>
        [HttpGet("{id}/resetPassword")]
        [SwaggerResponse(200, typeof(IS4.ApiResource), "The password was reset.")]
        [SwaggerResponse(404, typeof(Error), NotFoundErrorMsg)]
        [SwaggerResponse(400, typeof(Error), BadRequestErrorMsg)]
        public IActionResult ResetPassword(string id)
        {
            var apiResource = _apiResourceStore.GetResource(id);

            if (apiResource == null || string.IsNullOrEmpty(apiResource.Name))
            {
                return CreateFailureResponse($"The specified api resource with id: {id} was not found",
                    HttpStatusCode.NotFound);
            }

            // Update password
            var resourceSecret = GeneratePassword();
            apiResource.ApiSecrets = new List<IS4.Secret> {GetNewSecret(resourceSecret)};
            _apiResourceStore.UpdateResource(id, apiResource);

            // Prepare return values
            var viewApiResource = apiResource.ToApiResourceViewModel();
            viewApiResource.ApiSecret = resourceSecret;

            return Ok(viewApiResource);
        }

        /// <summary>
        ///     Creates an API resource.
        /// </summary>
        /// <param name="resource">The <see cref="IS4.ApiResource" /> object to add.</param>
        /// <returns></returns>
        [HttpPost]
        [SwaggerResponse(201, typeof(IS4.ApiResource), "The API resource was created.")]
        [SwaggerResponse(400, typeof(Error), BadRequestErrorMsg)]
        [SwaggerResponse(409, typeof(Error), DuplicateErrorMsg)]
        public IActionResult Post([FromBody] IS4.ApiResource resource)
        {
            return ValidateAndExecute(resource, () =>
            {
                var id = resource.Name;

                var existingResource = _apiResourceStore.GetResource(id);
                if (existingResource != null)
                {
                    return CreateFailureResponse(
                        $"Api resource {id} already exists. Please provide a new name",
                        HttpStatusCode.Conflict);
                }

                // override any secret in the request.
                // TODO: we need to implement a salt strategy, either at the controller level or store level.
                var resourceSecret = GeneratePassword();
                resource.ApiSecrets = new List<IS4.Secret> {GetNewSecret(resourceSecret)};
                _apiResourceStore.AddResource(resource);

                var viewResource = resource.ToApiResourceViewModel();
                viewResource.ApiSecret = resourceSecret;
                return CreatedAtAction("Get", new {id}, viewResource);
            });
        }

        /// <summary>
        ///     Modifies the API resource by <paramref name="id" />.
        /// </summary>
        /// <param name="id">The unique identifier of the API resource.</param>
        /// <param name="apiResource">The <see cref="IS4.ApiResource" /> object to update.</param>
        /// <returns></returns>
        [HttpPut("{id}")]
        [SwaggerResponse(204, null, "No Content")]
        [SwaggerResponse(404, typeof(Error), NotFoundErrorMsg)]
        [SwaggerResponse(400, typeof(Error), BadRequestErrorMsg)]
        [SwaggerResponse(409, typeof(Error), DuplicateErrorMsg)]
        public IActionResult Put(string id, [FromBody] IS4.ApiResource apiResource)
        {
            return ValidateAndExecute(apiResource, () =>
            {
                var storedApiResource = _apiResourceStore.GetResource(id);

                if (storedApiResource == null || string.IsNullOrEmpty(storedApiResource.Name))
                {
                    return CreateFailureResponse($"The specified api resource with id:{id} was not found",
                        HttpStatusCode.NotFound);
                }

                // Prevent from changing secrets.
                apiResource.ApiSecrets = storedApiResource.ApiSecrets;
                // Prevent from changing payload Name.
                apiResource.Name = id;

                _apiResourceStore.UpdateResource(id, apiResource);
                return NoContent();
            });
        }

        /// <summary>
        ///     Deletes the API resource by <paramref name="id" />.
        /// </summary>
        /// <param name="id">The unique identifier of the API resource.</param>
        /// <returns></returns>
        [HttpDelete("{id}")]
        [SwaggerResponse(204, null, "The specified API resource was deleted.")]
        [SwaggerResponse(404, typeof(Error), NotFoundErrorMsg)]
        public IActionResult Delete(string id)
        {
            var apiResource = _apiResourceStore.GetResource(id);

            if (apiResource == null)
            {
                return CreateFailureResponse($"The specified api resource with id: {id} was not found",
                    HttpStatusCode.NotFound);
            }

            _apiResourceStore.DeleteResource(id);
            return NoContent();
        }
    }
}