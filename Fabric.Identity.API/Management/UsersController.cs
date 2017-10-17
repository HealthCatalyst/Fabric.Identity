using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Fabric.Identity.API.Models;
using Fabric.Identity.API.Infrastructure.QueryStringBinding;
using Fabric.Identity.API.Services;
using Fabric.Identity.API.Validation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Serilog;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Fabric.Identity.API.Management
{
    /// <summary>
    /// Find Users registered in Identity.
    /// </summary>
    [Authorize(Policy = "ReadScopeClaim", ActiveAuthenticationSchemes = "Bearer")]
    [ApiVersion("1.0")]
    [Route("api/users")]
    [Route("api/v{version:apiVersion}/users")]
    public class UsersController : BaseController<UserApiModel>
    {
        private readonly IDocumentDbService _documentDbService;
        private readonly IExternalIdentityProviderServiceResolver _externalIdentityProviderServiceResolver;

        public UsersController(IDocumentDbService documentDbService, IExternalIdentityProviderServiceResolver externalIdentityProviderServiceResolver, UserApiModelValidator validator, ILogger logger) 
            : base(validator, logger)
        {
            _documentDbService = documentDbService ?? throw new ArgumentNullException(nameof(documentDbService));
            _externalIdentityProviderServiceResolver = externalIdentityProviderServiceResolver ??
                                               throw new ArgumentNullException(nameof(externalIdentityProviderServiceResolver));
        }
        
        /// <summary>
        /// Find users by client id and user id
        /// </summary>
        /// <param name="clientId">The client id to find users for</param>
        /// <param name="userIds">The user ids for the users requested in the format 'subjectid:provider'</param>
        /// <returns></returns>
        [HttpGet]
        [SwaggerResponse((int)HttpStatusCode.OK, typeof(List<UserApiModel>), "Success")]
        [SwaggerResponse((int)HttpStatusCode.BadRequest, typeof(Error), BadRequestErrorMsg)]
        [SwaggerResponse((int)HttpStatusCode.NotFound, typeof(Error), "The specified client id could not be found")]
        public async Task<IActionResult> Get(string clientId, [CommaSeparated] IEnumerable<string> userIds)
        {
            return await ProcessSearchRequest(clientId, userIds).ConfigureAwait(false);
        }

        /// <summary>
        /// Find users by client id and user id
        /// </summary>
        /// <param name="searchParameters">The <see cref="UserSearchParameter"/> containing the client id and user ids in the format 'subjectid:provider'</param>
        /// <returns></returns>
        [HttpPost]
        [SwaggerResponse((int)HttpStatusCode.OK, typeof(List<UserApiModel>), "Success")]
        [SwaggerResponse((int)HttpStatusCode.BadRequest, typeof(Error), BadRequestErrorMsg)]
        [SwaggerResponse((int)HttpStatusCode.NotFound, typeof(Error), "The specified client id could not be found")]
        public async Task<IActionResult> Post([FromBody] UserSearchParameter searchParameters)
        {
            return await ProcessSearchRequest(searchParameters.ClientId, searchParameters.UserIds).ConfigureAwait(false);
        }

        /// <summary>
        /// Find users in the 3rd party identity provider (IDP), e.g. Active Directory, Azure Active Directory, etc..
        /// </summary>
        /// <param name="searchText">The portion of a user's name or username to search for in the 3rd party identity provider. We will search in the First Name, LastName, and Useranme fields as specified by the 3rd party IDP.</param>
        /// <param name="identityProvider">The source identity provider to search within.</param>
        /// <returns></returns>
        [HttpGet("search")]
        [SwaggerResponse((int)HttpStatusCode.OK, typeof(List<UserApiModel>), "Success")]
        [SwaggerResponse((int)HttpStatusCode.BadRequest, typeof(Error), BadRequestErrorMsg)]
        public IActionResult Search(string searchText, string identityProvider)
        {
            try
            {
                var externalIdentityProvider =
                    _externalIdentityProviderServiceResolver.GetExternalIdentityProviderService(identityProvider);
                var users = externalIdentityProvider.SearchUsers(searchText);
                var apiUsers = users.Select(u => new UserApiModel
                {
                    FirstName = u.FirstName,
                    LastName = u.LastName,
                    MiddleName = u.MiddleName,
                    SubjectId = u.SubjectId
                });
                return Ok(apiUsers);
            }
            catch (InvalidOperationException e)
            {
                return CreateFailureResponse(e.Message, HttpStatusCode.BadRequest);
            }
            
        }

        private async Task<IActionResult> ProcessSearchRequest(string clientId, IEnumerable<string> userIds)
        {
            var docIds = userIds.Select(id => id?.ToLower()).ToList();
            if (!docIds.Any() || docIds.All(d => string.IsNullOrEmpty(d)))
            {
                return CreateFailureResponse("No userIds were included in the request",
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
