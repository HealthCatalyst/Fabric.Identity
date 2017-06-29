using System;
using IdentityModel;
using Microsoft.AspNetCore.Http;

namespace Fabric.Identity.API.Services
{
    public class UserResolverService : IUserResolveService
    {
        private readonly HttpContext _context;
        public UserResolverService(IHttpContextAccessor contextAccessor)
        {
            if (contextAccessor == null)
            {
                throw new ArgumentNullException(nameof(contextAccessor));
            }
            _context = contextAccessor.HttpContext;
        }

        public string Username => _context?.User?.Identity.Name;

        public string ClientId => _context?.User?.FindFirst(JwtClaimTypes.ClientId)?.Value;
        
        public string Subject => _context?.User?.FindFirst(JwtClaimTypes.Subject)?.Value;
    }
}
