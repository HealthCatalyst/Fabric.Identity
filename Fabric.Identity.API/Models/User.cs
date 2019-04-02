using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;

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
        public ICollection<Claim> Claims { get; set; } = new List<Claim>();
        public ICollection<UserLogin> LastLoginDatesByClient { get; set; } = new List<UserLogin>();

        public void SetLastLoginDateForClient(string clientId)
        {
            var clientIdToLog = clientId;
            if (string.IsNullOrEmpty(clientId))
            {
                clientIdToLog = FabricIdentityConstants.ServiceName;
            }

            var existingLogin =
                LastLoginDatesByClient.FirstOrDefault(l =>
                    l.ClientId.Equals(clientId, StringComparison.OrdinalIgnoreCase));

            if (existingLogin != null)
            {
                existingLogin.LoginDate = DateTime.UtcNow;
            }
            else
            {
                LastLoginDatesByClient.Add(new UserLogin {ClientId = clientIdToLog, LoginDate = DateTime.UtcNow});
            }
        }

        public override string ToString()
        {
            return
                $"{SubjectId} | {Username} | {FirstName} | {MiddleName} | {LastName} | {ProviderName} | {string.Join("|", Claims.Select(c => $"{c.Type}={c.Value}"))}";
        }
    }
}
