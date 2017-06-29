using System.Net;
using Fabric.Identity.API.Models;
using Fabric.Identity.API.Services;
using Fabric.Identity.API.Validation;
using IS4 = IdentityServer4.Models;
using Microsoft.AspNetCore.Mvc;
using Serilog;
using System.Linq;
using System.Collections.Generic;
using Microsoft.AspNetCore.Authorization;

namespace Fabric.Identity.API.Management
{
    [Authorize(Policy = "RegistrationThreshold", ActiveAuthenticationSchemes = "Bearer")]
    [Route("api/client")]
    public class ClientController : BaseController<IS4.Client>
    {
        private readonly IDocumentDbService _documentDbService;
        private const string GetClientRouteName = "GetClient";
        private const string ResetPasswordRouteName = "ResetClientPassword";

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

        // GET api/values/5/resetPassword
        [HttpGet("{id}/resetPassword", Name = ResetPasswordRouteName)]
        public IActionResult ResetPassword(string id)
        {
            var client = _documentDbService.GetDocument<IS4.Client>(id).Result;

            if (client == null || string.IsNullOrEmpty(client.ClientId))
            {
                return CreateFailureResponse($"The specified client with id: {id} was not found",
                    HttpStatusCode.NotFound);
            }

            // Update password
            client.ClientSecrets = new List<IS4.Secret>() { new IS4.Secret(GeneratePassword()) };
            _documentDbService.UpdateDocument(id, client);

            // Prepare return values
            var viewClient = client.ToClientViewModel();
            viewClient.ClientSecret = client.ClientSecrets.First().Value;

            return Ok(viewClient);
        }

        // POST api/values
        [HttpPost]
        public IActionResult Post([FromBody] IS4.Client client)
        {
            return ValidateAndExecute(client, () =>
            {
                var id = client.ClientId;

                // override any secret in the request.
                // TODO: we need to implement a salt strategy, either at the controller level or store level.
                var clientSecret = this.GeneratePassword();
                client.ClientSecrets = new List<IS4.Secret>() { new IS4.Secret(IS4.HashExtensions.Sha256(clientSecret)) };
                _documentDbService.AddDocument(id, client);

                Client viewClient = client.ToClientViewModel();
                viewClient.ClientSecret = clientSecret;

                return CreatedAtRoute(GetClientRouteName, new { id }, viewClient);
            });
        }

        // PUT api/values/5
        [HttpPut("{id}")]
        public IActionResult Put(string id, [FromBody] IS4.Client client)
        {
            return ValidateAndExecute(client, () =>
            {
                var storedClient = _documentDbService.GetDocument<IS4.Client>(id).Result;

                if (storedClient == null || string.IsNullOrEmpty(storedClient.ClientId))
                {
                    return CreateFailureResponse($"The specified client with id: {id} was not found",
                        HttpStatusCode.NotFound);
                }

                // Prevent from changing secrets.
                client.ClientSecrets = storedClient.ClientSecrets;
                // Prevent from changing payload ClientId.
                client.ClientId = id; 

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

