using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using AutoMapper;
using Fabric.Identity.API.Models;
using Fabric.Identity.API.Services;
using Fabric.Identity.API.Validation;
using Fabric.IdentityProviderSearchService.Validators;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Serilog;
using Swashbuckle.AspNetCore.Annotations;
using ValidationRuleSets = Fabric.Identity.API.FabricIdentityConstants.ValidationRuleSets;

namespace Fabric.Identity.API.Management
{
    /// <summary>
    /// Find Users or Groups either registered in Identity or from AD or AAD.
    /// </summary>
    [Authorize(Policy = FabricIdentityConstants.AuthorizationPolicyNames.SearchUsersScopeClaim, AuthenticationSchemes = "Bearer")]
    [ApiVersion("1.0")]
    [Route("api/principals")]
    [Route("api/v{version:apiVersion}/principals")]
    public class PrincipalsController : BaseController<PrincipalSearchRequest>
    {
        private readonly IPrincipalSearchService _principalSearchService;
        private readonly IMapper _mapper;

        public PrincipalsController(SearchRequestValidator validator, ILogger logger,
            IPrincipalSearchService principalSearchService, IMapper mapper) : base(validator, logger)
        {
            _principalSearchService = principalSearchService;
            _mapper = mapper;
        }

        /// <summary>
        /// Find Users and Groups in Identity, along with 3rd party identity providers (currently
        /// supports Active Directory and Azure Active Directory).
        /// </summary>
        /// <param name="searchRequest">Specifies what principal to search for (requires at least the search text).</param>
        /// <returns></returns>
        [HttpGet("search")]
        [SwaggerResponse((int)HttpStatusCode.OK, "Success", typeof(IEnumerable<FabricPrincipalApiModel>))]
        [SwaggerResponse((int)HttpStatusCode.BadRequest, BadRequestErrorMsg, typeof(Error))]
        public async Task<IActionResult> Search(PrincipalSearchRequest searchRequest)
        {
            return await ValidateAndExecuteAsync(searchRequest, async () =>
            {
                var principals = new List<FabricPrincipalApiModel>();

                string tenantInfo = null;

                if (!string.IsNullOrWhiteSpace(searchRequest.TenantId))
                {
                    tenantInfo = $", TenantId={searchRequest.TenantId}";
                }

                Logger.Information($"searching for principals with IdentityProvider={searchRequest.IdentityProvider}, PrincipalName={searchRequest.SearchText}, SearchType={searchRequest.Type}{tenantInfo}");

                var usersAndGroups = await _principalSearchService.SearchPrincipalsAsync(searchRequest.SearchText,
                    searchRequest.Type, FabricIdentityConstants.SearchTypes.Wildcard, searchRequest.IdentityProvider,
                    searchRequest.TenantId).ConfigureAwait(false);

                principals.AddRange(_mapper.Map<IEnumerable<FabricPrincipalApiModel>>(usersAndGroups));

                return Ok(new SearchResultApiModel<FabricPrincipalApiModel>
                {
                    Principals = principals,
                    ResultCount = principals.Count
                });
            }, $"{ValidationRuleSets.PrincipalSearch},{ValidationRuleSets.Default}");
        }

        /// <summary>
        /// Find Users and Groups in either Identity or the specified 3rd party identity provider (currently
        /// supports Active Directory and Azure Active Directory).
        /// </summary>
        /// <param name="searchRequest">Specifies what principal to search for (requires at least the search text).</param>
        /// <returns></returns>
        [HttpGet("{IdentityProvider}/search")]
        [SwaggerResponse((int)HttpStatusCode.OK, "Success", typeof(IEnumerable<FabricPrincipalApiModel>))]
        [SwaggerResponse((int)HttpStatusCode.BadRequest, BadRequestErrorMsg, typeof(Error))]
        public async Task<IActionResult> SearchByIdentityProvider(PrincipalSearchRequest searchRequest)
        {
            return await ValidateAndExecuteAsync(searchRequest, async () =>
                {
                    var principals = new List<FabricPrincipalApiModel>();

                    string tenantInfo = null;

                    if (!string.IsNullOrWhiteSpace(searchRequest.TenantId))
                    {
                        tenantInfo = $", TenantId={searchRequest.TenantId}";
                    }

                    Logger.Information($"searching for groups with IdentityProvider={searchRequest.IdentityProvider}, GroupName={searchRequest.SearchText}, SearchType={searchRequest.Type}{tenantInfo}");

                    var usersAndGroups = await _principalSearchService.SearchPrincipalsAsync(searchRequest.SearchText,
                        searchRequest.Type, FabricIdentityConstants.SearchTypes.Wildcard,
                        searchRequest.IdentityProvider, searchRequest.TenantId);

                    principals.AddRange(_mapper.Map<IEnumerable<FabricPrincipalApiModel>>(usersAndGroups));

                    return Ok(new SearchResultApiModel<FabricPrincipalApiModel>
                    {
                        Principals = principals,
                        ResultCount = principals.Count
                    });
                },
                $"{ValidationRuleSets.PrincipalSearch},{ValidationRuleSets.PrincipalIdentityProviderSearch},{ValidationRuleSets.Default}");
        }

        /// <summary>
        /// Find Groups in either Identity or the specified 3rd party identity provider (currently
        /// supports Active Directory and Azure Active Directory).
        /// </summary>
        /// <param name="searchRequest">Specifies what group to search for (only accepts GroupName, IdentityProvider and TenantId)</param>
        /// <returns></returns>
        [HttpGet("{IdentityProvider}/groups/{GroupName}")]
        [SwaggerResponse((int)HttpStatusCode.OK, "Success", typeof(IEnumerable<FabricGroupApiModel>))]
        [SwaggerResponse((int)HttpStatusCode.BadRequest, BadRequestErrorMsg, typeof(Error))]
        public async Task<IActionResult> SearchForGroups(PrincipalSearchRequest searchRequest)
        {
            searchRequest.Type = FabricIdentityEnums.PrincipalType.Group.ToString();

            return await ValidateAndExecuteAsync(searchRequest, async () =>
            {
                var principals = new List<FabricGroupApiModel>();

                string tenantInfo = null;

                if (!string.IsNullOrWhiteSpace(searchRequest.TenantId))
                {
                    tenantInfo = $", TenantId={searchRequest.TenantId}";
                }

                Logger.Information($"searching for groups with IdentityProvider={searchRequest.IdentityProvider}, GroupName={searchRequest.GroupName}, SearchType={searchRequest.Type}{tenantInfo}");

                var groups = (await _principalSearchService.SearchGroupsAsync(searchRequest.GroupName,
                        FabricIdentityConstants.SearchTypes.Exact, searchRequest.IdentityProvider, searchRequest.TenantId))
                    .Select(g =>
                    {
                        g.IdentityProvider = searchRequest.IdentityProvider;
                        return g;
                    });

                principals.AddRange(_mapper.Map<IEnumerable<FabricGroupApiModel>>(groups));

                return Ok(new SearchResultApiModel<FabricGroupApiModel>
                {
                    Principals = principals,
                    ResultCount = principals.Count
                });
            }, $"{ValidationRuleSets.PrincipalIdentityProviderSearch},{ValidationRuleSets.PrincipalGroupSearch},{ValidationRuleSets.Default}");
        }

        /// <summary>
        /// Finds the user specified by the provided SubjectId in Identity or one of the 3rd party identity
        /// providers (currently supports Active Directory and Azure Active Directory).
        /// </summary>
        /// <param name="searchRequest">Specifies what user to search for (only accepts SubjectId and TenantId).</param>
        /// <returns></returns>
        [HttpGet("user")]
        [SwaggerResponse((int)HttpStatusCode.OK, "Success", typeof(FabricPrincipalApiModel))]
        [SwaggerResponse((int)HttpStatusCode.BadRequest, BadRequestErrorMsg, typeof(Error))]
        public async Task<IActionResult> SearchForUser(PrincipalSearchRequest searchRequest)
        {
            return await ValidateAndExecuteAsync(searchRequest, async () =>
                {
                    string tenantInfo = null;

                    if (!string.IsNullOrWhiteSpace(searchRequest.TenantId))
                    {
                        tenantInfo = $", TenantId={searchRequest.TenantId}";
                    }

                    Logger.Information($"searching for user with subject id: {searchRequest.SubjectId}{tenantInfo}");

                    var user = await _principalSearchService.FindUserBySubjectIdAsync(searchRequest.SubjectId,
                        searchRequest.TenantId);

                    if (user == null)
                    {
                        return CreateFailureResponse(
                            $"User {searchRequest.SubjectId} not found.", HttpStatusCode.NotFound);
                    }

                    return Ok(_mapper.Map<FabricPrincipalApiModel>(user));
                },
                $"{ValidationRuleSets.PrincipalSubjectSearch},{ValidationRuleSets.Default}");
        }
    }
}
