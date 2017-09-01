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
    public class UserSetup
    {
        private readonly TestUserStore _testUserStore;
        private readonly DocumentDbUserStore _userStore;

        public UserSetup(TestUserStore testUserStore,
            DocumentDbUserStore userStore)
        {
            _testUserStore = testUserStore;
            _userStore = userStore;
        }

        public UserInfo SetupTestUser(string provider, string userId, List<Claim> claims)
        {
            //check if the external user is already provisioned
            var user = _testUserStore.FindByExternalProvider(provider, userId);
            if (user == null)
            {
                //this sample simply auto-provisions new external user
                //another common approach is to start a registrations workflow first
                user = _testUserStore.AutoProvisionUser(provider, userId, claims);
            }
            else
            {
                //update the role claims from the provider                
                user.Claims = UpdateClaims(user.Claims, claims);
            }

            return new UserInfo(user);
        }

        public async Task<UserInfo> SetupUser(string provider, string userId, List<Claim> claims, string clientId)
        {
            //check if the external user is already provisioned
            var user = await _userStore.FindByExternalProvider(provider, userId);
            if (user == null)
            {
                //this sample simply auto-provisions new external user
                //another common approach is to start a registrations workflow first
                user = _userStore.AddUser(provider, userId, claims);
            }
            else
            {
                //update the role claims from the provider               
                user.Claims = UpdateClaims(user.Claims, claims);
            }

            //update the user model with the login
            await _userStore.SetLastLogin(clientId, userId);

            return new UserInfo(user);
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
    }
}
