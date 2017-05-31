using System;
using System.Net;
using Fabric.Identity.API.CouchDb;
using Fabric.Identity.API.Models;
using Fabric.Identity.API.Services;
using Fabric.Identity.API.Validation;
using IS4 = IdentityServer4.Models;
using Microsoft.AspNetCore.Mvc;
using Serilog;
using System.Linq;
using System.Collections.Generic;

namespace Fabric.Identity.API.Management
{
    [Route("api/[controller]")]
    public class ClientController : BaseController<IS4.Client>
    {
        private readonly IDocumentDbService _documentDbService;
        private const string GetClientRouteName = "GetClient";
        private Func<string> GeneratePassword { get; set; } = () => Convert.ToBase64String(Guid.NewGuid().ToByteArray()).Substring(0, 16);

        public ClientController(IDocumentDbService documentDbService, ClientValidator validator, ILogger logger)
            : base(validator, logger)
        {
            _documentDbService = documentDbService;
        }

        // GET api/values/5
        [HttpGet("{id}", Name = GetClientRouteName)]
        public IActionResult Get(string id)
        {
            var client = _documentDbService.GetDocument<IS4.Client>(id).Result;

            if (client == null || string.IsNullOrEmpty(client.ClientId))
            {
                return CreateFailureResponse($"The specified client with id: {id} was not found",
                    HttpStatusCode.NotFound);
            }

            return Ok(client.ToClientViewModel());
        }

        // POST api/values
        [HttpPost]
        public IActionResult Post([FromBody] IS4.Client client)
        {
            return ValidateAndExecute(client, () =>
            {
                // override any secret in the request.
                client.ClientSecrets = new List<IS4.Secret>() { new IS4.Secret(this.GeneratePassword()) };
                var id = client.ClientId;
                _documentDbService.AddDocument(id, client);

                Client viewClient = client.ToClientViewModel();
                viewClient.Secret = client.ClientSecrets.First().Value;

                return CreatedAtRoute(GetClientRouteName, new { id }, viewClient);
            });
        }

        // PUT api/values/5
        [HttpPut("{id}")]
        public IActionResult Put(string id, [FromBody] IS4.Client client)
        {
            return ValidateAndExecute(client, () =>
            {
                client.ClientSecrets = null;
                _documentDbService.UpdateDocument(id, client);
                return NoContent();
            });
        }
        
        // DELETE api/values/5
        [HttpDelete("{id}")]
        public IActionResult Delete(string id)
        {
            _documentDbService.DeleteDocument<IS4.Client>(id);
            return NoContent();
        }
    }
}

