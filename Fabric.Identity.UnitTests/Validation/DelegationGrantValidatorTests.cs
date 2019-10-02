using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Security.Claims;
using Fabric.Identity.API.ExtensionGrantValidators;
using IdentityModel;
using IdentityServer4.Validation;
using Xunit;

namespace Fabric.Identity.UnitTests.Validation
{
    public class DelegationGrantValidatorTests
    {
        private DelegationGrantValidator _delegationGrantValidator;

        [Fact]
        public async void ValidateAsync_NullUserToken_InvalidGrant()
        {
            _delegationGrantValidator = new DelegationGrantValidator(new StubTokenValidator());
            var context = new ExtensionGrantValidationContext
            {
                Request = new ValidatedTokenRequest
                {
                    Raw = new NameValueCollection()
                }
            };

            await _delegationGrantValidator.ValidateAsync(context);
            Assert.Equal(OidcConstants.TokenErrors.InvalidGrant, context.Result.Error);
        }

        [Fact]
        public async void ValidateAsync_InvalidUserToken_InvalidGrant()
        {
            var stubTokenValidator = new StubTokenValidator
            {
                AccessTokenValidationResult = new TokenValidationResult
                {
                    IsError = true

                }
            };

            _delegationGrantValidator = new DelegationGrantValidator(stubTokenValidator);

            var rawDictionary = new NameValueCollection { { "token", "xyz" } };
            var context = new ExtensionGrantValidationContext
            {
                Request = new ValidatedTokenRequest
                {
                    Raw = rawDictionary
                }
            };

            await _delegationGrantValidator.ValidateAsync(context);
            Assert.Equal(OidcConstants.TokenErrors.InvalidGrant, context.Result.Error);
        }

        [Fact]
        public async void ValidateAsync_ValidUserToken_ValidGrant()
        {
            var stubTokenValidator = new StubTokenValidator();
            var claims = new List<Claim>
            {
                new Claim(JwtClaimTypes.Subject, "first.last"),
                new Claim(JwtClaimTypes.IdentityProvider, "Windows"),
                new Claim(JwtClaimTypes.Role, "role1"),
                new Claim(JwtClaimTypes.Role, "role2"),
                new Claim("groups", "group1"),
                new Claim("groups", "group2")
            };

            stubTokenValidator.AccessTokenValidationResult = new TokenValidationResult
            {
                IsError = false,
                Claims = claims
            };

            _delegationGrantValidator = new DelegationGrantValidator(stubTokenValidator);
            var rawDictionary = new NameValueCollection { { "token", "xyz" } };
            var context = new ExtensionGrantValidationContext
            {
                Request = new ValidatedTokenRequest
                {
                    Raw = rawDictionary
                }
            };

            await _delegationGrantValidator.ValidateAsync(context);

            var claimsPrincipal = context.Result.Subject;

            var returnedClaims = claimsPrincipal.Claims.ToList();
            Assert.Null(context.Result.Error);            
            Assert.True(claims.Count >= 6);
            Assert.Contains(claims[0], returnedClaims, new ClaimComparer(true));
            Assert.Contains(claims[1], returnedClaims, new ClaimComparer(true));
            Assert.Contains(claims[2], returnedClaims, new ClaimComparer(true));
            Assert.Contains(claims[3], returnedClaims, new ClaimComparer(true));
            Assert.Contains(claims[4], returnedClaims, new ClaimComparer(true));
            Assert.Contains(claims[5], returnedClaims, new ClaimComparer(true));
        }
    }
}