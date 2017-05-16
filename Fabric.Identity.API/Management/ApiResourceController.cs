using System;
using System.Net;
using Fabric.Identity.API.CouchDb;
using Fabric.Identity.API.Validation;
using IdentityServer4.Models;
using Microsoft.AspNetCore.Mvc;
using Serilog;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace Fabric.Identity.API.Management
{
    [Route("api/[controller]")]
    public class ApiResourceController : BaseController<ApiResource>
    {
        private readonly IDocumentDbService _documentDbService;
        private const string GetApiResourceRouteName = "GetApiResource";

        public ApiResourceController(IDocumentDbService documentDbService, ApiResourceValidator validator, ILogger logger) 
            : base(validator, logger)
        {
            _documentDbService = documentDbService;
        }

        // GET api/values/5
        [HttpGet("{id}", Name= GetApiResourceRouteName)]
        public IActionResult Get(string id)
        {
            try
            {
                var apiResource = _documentDbService.GetDocument<ApiResource>(id).Result;

                if (apiResource == null)
                {
                    return CreateFailureResponse($"The specified api resource with id: {id} was not found",
                        HttpStatusCode.NotFound);
                }

                return Ok(apiResource);

            }
            catch (Exception)
            {
                Logger.Error($"The specified api resource with id: {id} was not found.");
                throw;
            }
        }

        // POST api/values
        [HttpPost]
        public IActionResult Post([FromBody]ApiResource value)
        {
            try
            {
                var validationResult = Validate(value);

                if (!validationResult.IsValid)
                {
                    return CreateValidationFailureResponse(validationResult);
                }

                var id = value.Name;
                _documentDbService.AddDocument(id, value);

                return CreatedAtRoute(GetApiResourceRouteName, new {id}, value);
            }
            catch (Exception e)
            {
                Logger.Error($"Unable to create a new api resource. Error: {e.Message}");
                throw;
            }
        }

        // PUT api/values/5
        [HttpPut("{id}")]
        public IActionResult Put(string id, [FromBody]ApiResource value)
        {
            try
            {
                var validationResult = Validate(value);

                if (!validationResult.IsValid)
                {
                    return CreateValidationFailureResponse(validationResult);
                }

                _documentDbService.UpdateDocument(id, value);

                return NoContent();
            }
            catch (Exception e)
            {
                Logger.Error($"Unable to update api resource. Error: {e.Message}");
                throw;
            }
        }

        // DELETE api/values/5
        [HttpDelete("{id}")]
        public IActionResult Delete(string id)
        {
            try
            {
                _documentDbService.DeleteDocument<ApiResource>(id);

                return NoContent();
            }
            catch (Exception)
            {
                Logger.Error($"Unable to delete api resource with id: {id}");
                throw;
            }
        }
    }
}
