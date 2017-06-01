using System.Collections.Generic;
using System.Linq;
using System.Net;
using Fabric.Identity.API.Models;
using Fabric.Identity.API.Services;
using Fabric.Identity.API.Validation;
using Microsoft.AspNetCore.Mvc;
using Serilog;
using IS4 = IdentityServer4.Models;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace Fabric.Identity.API.Management
{
    [Route("api/[controller]")]
    public class ApiResourceController : BaseController<IS4.ApiResource>
    {
        private readonly IDocumentDbService _documentDbService;
        private const string GetApiResourceRouteName = "GetApiResource";
        private const string ResetPasswordRouteName = "ResetApiResourcePassword";

        public ApiResourceController(IDocumentDbService documentDbService, ApiResourceValidator validator, ILogger logger)
            : base(validator, logger)
        {
            _documentDbService = documentDbService;
        }

        // GET api/values/5
        [HttpGet("{id}", Name = GetApiResourceRouteName)]
        public IActionResult Get(string id)
        {
            var apiResource = _documentDbService.GetDocument<IS4.ApiResource>(id).Result;

            if (apiResource == null)
            {
                return CreateFailureResponse($"The specified api resource with id: {id} was not found",
                    HttpStatusCode.NotFound);
            }

            return Ok(apiResource.ToApiResourceViewModel());
        }

        // GET api/values/5/resetPassword
        [HttpGet("{id}/resetPassword", Name = ResetPasswordRouteName)]
        public IActionResult ResetPassword(string id)
        {
            var apiResource = _documentDbService.GetDocument<IS4.ApiResource>(id).Result;

            if (apiResource == null || string.IsNullOrEmpty(apiResource.Name))
            {
                return CreateFailureResponse($"The specified api resource with id: {id} was not found",
                    HttpStatusCode.NotFound);
            }

            // Update password
            apiResource.ApiSecrets = new List<IS4.Secret>() { new IS4.Secret(GeneratePassword()) };
            _documentDbService.UpdateDocument(id, apiResource);

            // Prepare return values
            var viewApiResource = apiResource.ToApiResourceViewModel();
            viewApiResource.ApiSecret = apiResource.ApiSecrets.First().Value;

            return Ok(viewApiResource);
        }

        // POST api/values
        [HttpPost]
        public IActionResult Post([FromBody] IS4.ApiResource resource)
        {
            return ValidateAndExecute(resource, () =>
            {
                var id = resource.Name;
                resource.ApiSecrets = new List<IS4.Secret>() { new IS4.Secret(GeneratePassword()) };
                _documentDbService.AddDocument(id, resource);

                var viewResource = resource.ToApiResourceViewModel();
                viewResource.ApiSecret = resource.ApiSecrets.First().Value;
                return CreatedAtRoute(GetApiResourceRouteName, new { id }, viewResource);
            });
        }

        // PUT api/values/5
        [HttpPut("{id}")]
        public IActionResult Put(string id, [FromBody] IS4.ApiResource apiResource)
        {
            return ValidateAndExecute(apiResource, () =>
            {
                var storedApiResource = _documentDbService.GetDocument<IS4.ApiResource>(id).Result;

                if (storedApiResource == null || string.IsNullOrEmpty(storedApiResource.Name))
                {
                    return CreateFailureResponse($"The specified api resource with id:{id} was not found",
                        HttpStatusCode.NotFound);
                }

                // Prevent from changing secrets.
                apiResource.ApiSecrets = storedApiResource.ApiSecrets;
                // Prevent from changing payload Name.
                apiResource.Name = id;

                _documentDbService.UpdateDocument(id, apiResource);
                return NoContent();
            });
        }

        // DELETE api/values/5
        [HttpDelete("{id}")]
        public IActionResult Delete(string id)
        {
            _documentDbService.DeleteDocument<IS4.ApiResource>(id);
            return NoContent();
        }
    }
}