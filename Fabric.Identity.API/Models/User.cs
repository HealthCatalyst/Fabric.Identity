using System;
using System.Collections.Generic;
using System.Security.Claims;
using IdentityServer4.Test;

namespace Fabric.Identity.API.Models
{
    public class User
    {
        public string SubjectId { get; set; }
        public string Username { get; set; }
        public string FirstName { get; set; }
        public string MiddleName { get; set; }
        public string LastName { get; set; }
        public string ProviderName { get; set; }
        public ICollection<Claim> Claims { get; set; }
        public Dictionary<string, DateTime> LastLoginDatesByClient { get; } = new Dictionary<string, DateTime>();

        public void SetLastLoginDateForClient(string clientId)
        {
            var clientIdToLog = clientId;
            if (string.IsNullOrEmpty(clientId))
            {
                clientIdToLog = FabricIdentityConstants.ServiceName;
            }

            if (LastLoginDatesByClient.ContainsKey(clientIdToLog))
            {
                LastLoginDatesByClient.Remove(clientIdToLog);
            }

            LastLoginDatesByClient.Add(clientIdToLog, DateTime.UtcNow);
        }       
    }

    public static class TestUserExtensions
    {
        public static User ToUser(this TestUser testUser)
        {
            return new User
            {
                SubjectId = testUser.SubjectId,
                ProviderName = testUser.ProviderName,
                Username = testUser.Username,
                Claims = testUser.Claims,
            };
        }
    }
}
