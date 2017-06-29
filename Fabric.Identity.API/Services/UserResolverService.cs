using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using IdentityModel;
using Microsoft.AspNetCore.Http;

namespace Fabric.Identity.API.Services
{
    public class UserResolverService : IUserResolveService
    {
        private readonly HttpContext _context;
        public UserResolverService(IHttpContextAccessor contextAccessor)
        {
            var contextAccessorLocal = contextAccessor ?? throw new ArgumentNullException(nameof(contextAccessor));
            _context = contextAccessorLocal.HttpContext;
        }

        public string Username => _context?.User?.Identity.Name;

        public string ClientId => _context?.User?.FindFirst(JwtClaimTypes.ClientId)?.Value;
        
        public string Subject => _context?.User?.FindFirst(JwtClaimTypes.Subject)?.Value;
    }
}
