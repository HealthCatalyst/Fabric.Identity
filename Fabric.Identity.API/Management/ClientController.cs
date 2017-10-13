using System.Net;
using Fabric.Identity.API.Models;
using Fabric.Identity.API.Services;
using Fabric.Identity.API.Validation;
using IS4 = IdentityServer4.Models;
using Microsoft.AspNetCore.Mvc;
using Serilog;
using System.Linq;
using System.Collections.Generic;
using Fabric.Identity.API.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Fabric.Identity.API.Management
{
    /// <summary>
    /// Manage client applications.
    /// </summary>
    [Authorize(Policy = "RegistrationThreshold", ActiveAuthenticationSchemes = "Bearer")]
    [ApiVersion("1.0")]
    [Route("api/client")]
    [Route("api/v{version:apiVersion}/client")]
    public class ClientController : BaseController<IS4.Client>
    {
        private const string NotFoundErrorMsg = "The specified client id could not be found.";

        private readonly IDocumentDbService _documentDbService;

        /// <summary>
        /// Manage client applications (aka relying parties) in Fabric.Identity. 
        /// </summary>
        /// <param name="documentDbService"></param>
        /// <param name="validator"></param>
        /// <param name="logger"></param>
        public ClientController(IDocumentDbService documentDbService, ClientValidator validator, ILogger logger)
            : base(validator, logger)
        {
            _documentDbService = documentDbService;
        }

        /// <summary>
        /// Find a client by id
        /// </summary>
        /// <param name="id">The unique identifier of the client.</param>
        /// <returns></returns>
        [HttpGet("{id}")]
        [SwaggerResponse(200, typeof(Client), "Success")]
        [SwaggerResponse(404, typeof(Error), NotFoundErrorMsg)]
        [SwaggerResponse(400, typeof(Error), BadRequestErrorMsg)]
        public IActionResult Get(string id)
        {
            var client = _documentDbService.GetDocument<IS4.Client>(id).Result;

            if (string.IsNullOrEmpty(client?.ClientId))
            {
                return CreateFailureResponse($"The specified client with id: {id} was not found",
                    HttpStatusCode.NotFound);
            }

            return Ok(client.ToClientViewModel());
        }

        /// <summary>
        /// Reset a client secret
        /// </summary>
        /// <param name="id">The unique id of the client to reset.</param>
        /// <returns></returns>
        [HttpGet("{id}/resetPassword")]
        [SwaggerResponse(200, typeof(Client), "The secret for the client has been reset.")]
        [SwaggerResponse(404, typeof(Error), NotFoundErrorMsg)]
        [SwaggerResponse(400, typeof(Error), BadRequestErrorMsg)]
        public IActionResult ResetPassword(string id)
        {
            var client = _documentDbService.GetDocument<IS4.Client>(id).Result;

            if (string.IsNullOrEmpty(client?.ClientId))
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

        /// <summary>
        /// Add a client
        /// </summary>
        /// <param name="client">The <see cref="IS4.Client"/> object to add.</param>
        /// <returns></returns>
        [HttpPost]
        [SwaggerResponse(201, typeof(Client), "The client was created.")]
        [SwaggerResponse(400, typeof(Error), BadRequestErrorMsg)]
        public IActionResult Post([FromBody] Client client)
        {
            try
            {
                var is4Client = client.ToIs4ClientModel();
                return ValidateAndExecute(is4Client, () =>
                {
                    var id = is4Client.ClientId;

                    // override any secret in the request.
                    // TODO: we need to implement a salt strategy, either at the controller level or store level.
                    var clientSecret = this.GeneratePassword();
                    is4Client.ClientSecrets =
                        new List<IS4.Secret>() {new IS4.Secret(IS4.HashExtensions.Sha256(clientSecret))};
                    _documentDbService.AddDocument(id, is4Client);

                    var viewClient = is4Client.ToClientViewModel();
                    viewClient.ClientSecret = clientSecret;

                    return CreatedAtAction("Get", new {id}, viewClient);
                });
            }
            catch (BadRequestException<Client> ex)
            {
                return CreateFailureResponse(ex.Message, HttpStatusCode.BadRequest);
            }
        }

        /// <summary>
        /// Update a client
        /// </summary>
        /// <param name="id">The unique id of the client to update.</param>
        /// <param name="client">The <see cref="IS4.Client"/> object to update.</param>
        /// <returns></returns>
        [HttpPut("{id}")]
        [SwaggerResponse(204, typeof(void), "The specified client was updated.")]
        [SwaggerResponse(404, typeof(Error), NotFoundErrorMsg)]
        [SwaggerResponse(400, typeof(Error), BadRequestErrorMsg)]
        public IActionResult Put(string id, [FromBody] Client client)
        {
            var is4Client = client.ToIs4ClientModel();
            return ValidateAndExecute(is4Client, () =>
            {
                var storedClient = _documentDbService.GetDocument<IS4.Client>(id).Result;

                if (string.IsNullOrEmpty(storedClient?.ClientId))
                {
                    return CreateFailureResponse($"The specified client with id: {id} was not found",
                        HttpStatusCode.NotFound);
                }

                // Prevent from changing secrets.
                is4Client.ClientSecrets = storedClient.ClientSecrets;
                // Prevent from changing payload ClientId.
                is4Client.ClientId = id; 

                _documentDbService.UpdateDocument(id, is4Client);
                return NoContent();
            });
        }

        /// <summary>
        /// Delete a client
        /// </summary>
        /// <param name="id">The unique id of the client to delete.</param>
        /// <returns></returns>
        [HttpDelete("{id}")]
        [SwaggerResponse(204, typeof(void), "The specified client was deleted.")]
        [SwaggerResponse(404, typeof(Error), NotFoundErrorMsg)]
        public IActionResult Delete(string id)
        {
            _documentDbService.DeleteDocument<IS4.Client>(id);
            return NoContent();
        }
    }
}

