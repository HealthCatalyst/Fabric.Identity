using System.Net;
using Fabric.Identity.API.CouchDb;
using Fabric.Identity.API.Validation;
using IdentityServer4.Models;
using Microsoft.AspNetCore.Mvc;
using Serilog;

namespace Fabric.Identity.API.Management
{
    [Route("api/[controller]")]
    public class ClientController : BaseController<Client>
    {
        private readonly IDocumentDbService _documentDbService;
        private const string GetClientRouteName = "GetClient";

        public ClientController(IDocumentDbService documentDbService, ClientValidator validator, ILogger logger)
            : base(validator, logger)
        {
            _documentDbService = documentDbService;
        }

        // GET api/values/5
        [HttpGet("{id}", Name = GetClientRouteName)]
        public IActionResult Get(string id)
        {
            var client = _documentDbService.GetDocument<Client>(id).Result;

            if (client == null)
            {
                return CreateFailureResponse($"The specified client with id: {id} was not found",
                    HttpStatusCode.NotFound);
            }

            return Ok(client);
        }

        // POST api/values
        [HttpPost]
        public IActionResult Post([FromBody] Client value)
        {
            var validationResult = Validate(value);

            if (!validationResult.IsValid)
            {
                return CreateValidationFailureResponse(validationResult);
            }

            var id = value.ClientId;
            _documentDbService.AddDocument(id, value);

            return CreatedAtRoute(GetClientRouteName, new {id}, value);
        }

        // PUT api/values/5
        [HttpPut("{id}")]
        public IActionResult Put(string id, [FromBody] Client value)
        {
            var validationResult = Validate(value);

            if (!validationResult.IsValid)
            {
                return CreateValidationFailureResponse(validationResult);
            }
            _documentDbService.UpdateDocument(id, value);

            return NoContent();
        }

        // DELETE api/values/5
        [HttpDelete("{id}")]
        public IActionResult Delete(string id)
        {
            _documentDbService.DeleteDocument<Client>(id);
            return NoContent();
        }
    }
}

