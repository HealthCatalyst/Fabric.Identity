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
    public class IdentityResourceController : BaseController<IdentityResource>
    {
        private readonly IDocumentDbService _documentDbService;
        private const string GetIdentityResourceRouteName = "GetIdentityResource";
        
        public IdentityResourceController(IDocumentDbService documentDbService, IdentityResourceValidator validator, ILogger logger) 
            : base(validator, logger)
        {
            _documentDbService = documentDbService;
        }

        // GET api/values/5
        [HttpGet("{id}", Name = GetIdentityResourceRouteName)]
        public IActionResult Get(string id)
        {
            try
            {
                var identityResource = _documentDbService.GetDocument<IdentityResource>(id).Result;

                if (identityResource == null)
                {
                    return CreateFailureResponse($"The specified client with id: {id} was not found",
                        HttpStatusCode.NotFound);
                }
                return Ok(identityResource);
            }
            catch (Exception)
            {
                Logger.Error($"The specified identity resource with id: {id} was not found.");
                throw;
            }
        }

        // POST api/values
        [HttpPost]
        public IActionResult Post([FromBody]IdentityResource value)
        {
            try
            {
                var validationResult = Validate(value);

                if (!validationResult.IsValid)
                {
                    return CreateValidationFailureResponse(validationResult);
                }

                var id = value.Name;
                _documentDbService.AddOrUpdateDocument(id, value);

                return CreatedAtRoute(GetIdentityResourceRouteName, new {id}, value);
            }
            catch (Exception e)
            {
                Logger.Error($"Unable to create a new identity resource. Error: {e.Message}");
                throw;
            }
        }

        // PUT api/values/5
        [HttpPut("{id}")]
        public IActionResult Put(string id, [FromBody]IdentityResource value)
        {
            try
            {
                var validationResult = Validate(value);

                if (!validationResult.IsValid)
                {
                    return CreateValidationFailureResponse(validationResult);
                }
                _documentDbService.AddOrUpdateDocument(id, value);

                return NoContent();
            }
            catch (Exception e)
            {
                Logger.Error($"Unable to update identity resource. Error: {e.Message}");
                throw;
            }
        }

        // DELETE api/values/5
        [HttpDelete("{id}")]
        public IActionResult Delete(string id)
        {
            try
            {
                _documentDbService.DeleteDocument<IdentityResource>(id);
                return NoContent();
            }
            catch (Exception)
            {
                Logger.Error($"Unable to delete identity resource with id: {id}");
                throw;
            }
        }
    }
}
