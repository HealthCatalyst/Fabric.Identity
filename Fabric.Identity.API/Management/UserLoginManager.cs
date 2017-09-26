using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Fabric.Identity.API.DocumentDbStores;
using Fabric.Identity.API.Models;
using IdentityModel;
using IdentityServer4.Test;
using Serilog;

namespace Fabric.Identity.API.Management
{
    public class UserLoginManager
    {
        private readonly TestUserStore _testUserStore;
        private readonly DocumentDbUserStore _userStore;
        private readonly ILogger _logger;

        public UserLoginManager(TestUserStore testUserStore,
            DocumentDbUserStore userStore,
            ILogger logger)
        {
            _testUserStore = testUserStore;
            _userStore = userStore;
            _logger = logger;
        }

        public User TestUserLogin(string provider, string userId, List<Claim> claims)
        {
            //check if the external user is already provisioned
            var testUser = _testUserStore.FindByExternalProvider(provider, userId);
            if (testUser == null)
            {
                //this sample simply auto-provisions new external user
                //another common approach is to start a registrations workflow first
                testUser = _testUserStore.AutoProvisionUser(provider, userId, claims);
            }
            else
            {
                //update the role claims from the provider                
                testUser.Claims = FilterClaims(claims);
            }

            return testUser.ToUser();
        }

        public async Task<User> UserLogin(string provider, string subjectId, List<Claim> claims, string clientId)
        {
            //check if the external user is already provisioned
            var user = await _userStore.FindByExternalProvider(provider, subjectId);
            if (user == null)
            {
                _logger.Information($"user was not found. subjectId: {subjectId} provider: {provider}");
                user = CreateNewUser(provider, subjectId, claims, clientId);
                _userStore.AddUser(user);
                return user;
            }
            
            //update certain user information on every login       
            user.Claims = FilterClaims(claims);
            SetNamePropertiesFromClaims(user);
            user.SetLastLoginDateForClient(clientId);       
            _userStore.UpdateUser(user);

            return user;
        }

        private void SetNamePropertiesFromClaims(User user)
        {
            foreach (var userClaim in user.Claims)
            {
                if (userClaim.Type == JwtClaimTypes.Name)
                {
                    user.Username = userClaim.Value;
                }
                else if (userClaim.Type == JwtClaimTypes.GivenName)
                {
                    user.FirstName = userClaim.Value;
                }
                else if (userClaim.Type == JwtClaimTypes.FamilyName)
                {
                    user.LastName = userClaim.Value;
                }
                else if (userClaim.Type == JwtClaimTypes.MiddleName)
                {
                    user.MiddleName = userClaim.Value;
                }
            }
        }

        private User CreateNewUser(string provider, string subjectId, IEnumerable<Claim> claims, string clientId)
        {
            var filteredClaims = FilterClaims(claims);

            // create new user
            var user = new User
            {
                SubjectId = subjectId,            
                ProviderName = provider,
                Claims = filteredClaims
            };
            SetNamePropertiesFromClaims(user);
            user.SetLastLoginDateForClient(clientId);

            return user;
        }

        private List<Claim> FilterClaims(IEnumerable<Claim> incomingClaims)
        {
            var filtered = new List<Claim>();

            foreach (var claim in incomingClaims)
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

            return filtered;
        }

        private void SetNameClaim(List<Claim> filtered)
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
    }
}
