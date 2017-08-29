using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Fabric.Identity.API.Models;
using Fabric.Identity.API.Services;
using IdentityModel;
using RestSharp.Extensions.MonoHttp;
using Serilog;

namespace Fabric.Identity.API.DocumentDbStores  
{
    public class DocumentDbUserStore
    {
        private readonly IDocumentDbService _documentDbService;
        private readonly ILogger _logger;

        public DocumentDbUserStore(IDocumentDbService documentDbService, ILogger logger)
        {
            _documentDbService = documentDbService;
            _logger = logger;
        }

        //id for a  user stored in documentDb = user:subjectid:provider

        public async Task<User> FindBySubjectId(string subjectId)
        {
            var encodedSubjectId = UrlEncodeString(subjectId);
            _logger.Information($"finding user with subject id: {encodedSubjectId}");
            var user = await _documentDbService.GetDocuments<User>($"{FabricIdentityConstants.DocumentTypes.UserDocumentType}{encodedSubjectId}");
            return user?.FirstOrDefault();
        }

        public async Task<User> FindByExternalProvider(string provider, string subjectId)
        {
            var encodedProvider = UrlEncodeString(provider);
            var encodedSubjectId = UrlEncodeString(subjectId);

            _logger.Information($"finding user with subject id: {encodedSubjectId} and provider: {encodedProvider}");
            var user = await _documentDbService.GetDocuments<User>($"{FabricIdentityConstants.DocumentTypes.UserDocumentType}{encodedSubjectId}:{encodedProvider}");
            return user?.FirstOrDefault();
        }

        public User AddUser(string provider, string subjectId, IEnumerable<Claim> claims)
        {
            // create a list of claims that we want to transfer into our store
            var filtered = new List<Claim>();

            foreach (var claim in claims)
            {
                // if the external system sends a display name - translate that to the standard OIDC name claim
                if (claim.Type == ClaimTypes.Name)
                {
                    filtered.Add(new Claim(JwtClaimTypes.Name, claim.Value));
                }
                // if the JWT handler has an outbound mapping to an OIDC claim use that
                else if (JwtSecurityTokenHandler.DefaultOutboundClaimTypeMap.ContainsKey(claim.Type))
                {
                    filtered.Add(new Claim(JwtSecurityTokenHandler.DefaultOutboundClaimTypeMap[claim.Type], claim.Value));
                }
                // copy the claim as-is
                else
                {
                    filtered.Add(claim);
                }
            }

            // if no display name was provided, try to construct by first and/or last name
            if (filtered.All(x => x.Type != JwtClaimTypes.Name))
            {
                SetNameClaim(filtered);
            }

            // check if a display name is available, otherwise fallback to subject id
            var name = filtered.FirstOrDefault(c => c.Type == JwtClaimTypes.Name)?.Value ?? subjectId;

            // create new user
            var user = new User
            {
                SubjectId = subjectId,
                Username = name,
                ProviderName = provider,
                Claims = filtered
            };

            var encodedProvider = UrlEncodeString(provider);
            var encodedSubjectId = UrlEncodeString(subjectId);
            _documentDbService.AddDocument($"{encodedSubjectId}:{encodedProvider}", user);
            _logger.Information($"added user: {name}");

            return user;

        }

        private static void SetNameClaim(List<Claim> filtered)
        {
            var first = filtered.FirstOrDefault(x => x.Type == JwtClaimTypes.GivenName)?.Value;
            var last = filtered.FirstOrDefault(x => x.Type == JwtClaimTypes.FamilyName)?.Value;
            if (first != null && last != null)
            {
                filtered.Add(new Claim(JwtClaimTypes.Name, first + " " + last));
            }
            else if (first != null)
            {
                filtered.Add(new Claim(JwtClaimTypes.Name, first));
            }
            else if (last != null)
            {
                filtered.Add(new Claim(JwtClaimTypes.Name, last));
            }
        }

        public async Task SetLastLogin(string clientId, string subjectId)
        {
            var user = await FindBySubjectId(subjectId).ConfigureAwait(false);

            if (user == null)
            {
                _logger.Information($"did not find a user with subject id {subjectId}");
                return;
            }

            user.SetLastLoginDateByClient(clientId);
            _logger.Information($"setting login date for user: {user.Username} and provider: {user.ProviderName}");

            _documentDbService.UpdateDocument($"{user.SubjectId}:{user.ProviderName}", user);
        }

        private string UrlEncodeString(string unencoded)
        {
            return HttpUtility.UrlEncode(unencoded);
        }
    }
}
