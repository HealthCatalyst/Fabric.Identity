using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using IdentityModel.Client;

namespace Fabric.Identity.API.Models
{
    public class TokenResponseWrapper
    {
        public DateTime ExpiryTime { get; set; }
        public TokenResponse Response { get; set; }
    }
}
