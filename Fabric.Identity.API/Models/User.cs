using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Fabric.Identity.API.DocumentDbStores;
using IdentityModel;
using IdentityServer4.Test;

namespace Fabric.Identity.API.Models
{
    public class User
    {
        public string SubjectId { get; set; }
        public string Username { get; set; }
        public string ProviderName { get; set; }
        public ICollection<Claim> Claims { get; set; }
        public Dictionary<string, DateTime> LatestLoginsByClient { get; } = new Dictionary<string, DateTime>();

        public void SetLastLoginDateByClient(string clientId)
        {
            if (LatestLoginsByClient.ContainsKey(clientId))
            {
                LatestLoginsByClient.Remove(clientId);
            }

            LatestLoginsByClient.Add(clientId, DateTime.UtcNow);
        }
    }

    public class UserFunctions
    {
        private readonly TestUserStore _testUserStore;
        private readonly DocumentDbUserStore _userStore;

        public UserFunctions(TestUserStore testUserStore,
            DocumentDbUserStore userStore)
        {
            _testUserStore = testUserStore;
            _userStore = userStore;
        }

        public TestUser FindTestUserByExternalProvider(string provider, string userId)
        {
            return _testUserStore.FindByExternalProvider(provider, userId);
        }

        public async Task<User> FindUserByExternalProvider(string provider, string userId)
        {
            return await _userStore.FindByExternalProvider(provider, userId);
        }

        public TestUser AddTestUser(string provider, string userId, List<Claim> claims)
        {
            return _testUserStore.AutoProvisionUser(provider, userId, claims);
        }

        public User AddUser(string provider, string userId, List<Claim> claims)
        {
            return _userStore.AddUser(provider, userId, claims);
        }

        public void UpdateTestUserRoleClaims(TestUser testUser, List<Claim> claims)
        {
            testUser.Claims = UpdateClaims(testUser.Claims, claims);
        }

        public void UpdateUserRoleClaims(User user, List<Claim> claims)
        {
            user.Claims = UpdateClaims(user.Claims, claims);
        }

        private ICollection<Claim> UpdateClaims(ICollection<Claim> existingClaims, ICollection<Claim> updatedClaims)
        {
            //if the provider sent us role claims, use those and remove any other role
            //claims from the user
            if (!updatedClaims.Any(c => c.Type == JwtClaimTypes.Role))
            {
                return existingClaims;
            }

            //update the role claims from the provider
            var roleClaimsFromProvider = updatedClaims.Where(c => c.Type == JwtClaimTypes.Role);
            var originalRoleClaims = existingClaims.Where(c => c.Type == JwtClaimTypes.Role).ToList();
            foreach (var originalRoleClaim in originalRoleClaims)
            {
                existingClaims.Remove(originalRoleClaim);
            }
            foreach (var roleClaim in roleClaimsFromProvider)
            {
                existingClaims.Add(roleClaim);
            }                

            return existingClaims;
        }

        public async Task SetLastLogin(string clientId, string subjectId)
        {
            await _userStore.SetLastLogin(clientId, subjectId);
        }

    }
}
