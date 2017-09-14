using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Fabric.Identity.API.Models;
using Fabric.Identity.API.Services;
using Fabric.Identity.API.Validation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Serilog;

namespace Fabric.Identity.API.Management
{
    
    [Authorize(Policy = "ReadScopeClaim", ActiveAuthenticationSchemes = "Bearer")]
    [ApiVersion("1.0")]
    [Route("api/users")]
    [Route("api/v{version:apiVersion}/users")]
    public class UsersController : BaseController<UserApiModel>
    {
        private readonly IDocumentDbService _documentDbService;

        public UsersController(IDocumentDbService documentDbService, UserApiModelValidator validator, ILogger logger) 
            : base(validator, logger)
        {
            _documentDbService = documentDbService;
        }

        [HttpGet]
        public async Task<IActionResult> Get(string clientId, IEnumerable<string> documentIds)
        {
            return await ProcessSearchRequest(clientId, documentIds);
        }

        [HttpPost]
        public async Task<IActionResult> Post([FromBody] UserSearchParameter searchParameters)
        {
            return await ProcessSearchRequest(searchParameters.ClientId, searchParameters.DocumentIds);
        }

        private async Task<IActionResult> ProcessSearchRequest(string clientId, IEnumerable<string> documentIds)
        {
            var docIds = documentIds.ToList();
            if (!docIds.Any())
            {
                return CreateFailureResponse("No documentIds were included in the request",
                    HttpStatusCode.BadRequest);
            }

            var client = _documentDbService.GetDocument<IdentityServer4.Models.Client>(clientId).Result;

            if (string.IsNullOrEmpty(client?.ClientId))
            {
                return CreateFailureResponse($"The specified client with id: {clientId} was not found",
                    HttpStatusCode.NotFound);
            }

            var users = await _documentDbService.GetDocumentsById<User>(docIds);

            return Ok(users.Select(u => u.ToUserViewModel(clientId)));
        }
    }    
}
