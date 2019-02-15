using System.Threading.Tasks;
using IdentityServer4.Models;
using IdentityServer4.Validation;

namespace Fabric.Identity.UnitTests.Validation
{
    public class StubTokenValidator : ITokenValidator
    {
        public TokenValidationResult AccessTokenValidationResult { get; set; } = new TokenValidationResult();
        public TokenValidationResult IdentityTokenValidationResult { get; set; } = new TokenValidationResult();
        public TokenValidationResult RefreshTokenValidationResult { get; set; } = new TokenValidationResult();

        public Task<TokenValidationResult> ValidateAccessTokenAsync(string token, string expectedScope = null)
        {
            return Task.FromResult(AccessTokenValidationResult);
        }

        public Task<TokenValidationResult> ValidateRefreshTokenAsync(string token, Client client = null)
        {
            return Task.FromResult(RefreshTokenValidationResult);
        }

        public Task<TokenValidationResult> ValidateIdentityTokenAsync(string token, string clientId = null,
            bool validateLifetime = true)
        {
            return Task.FromResult(IdentityTokenValidationResult);
        }
    }
}