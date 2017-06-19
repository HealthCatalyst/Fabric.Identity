using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Fabric.Identity.API.Infrastructure;
using IdentityServer4.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Serilog;
using Xunit;

namespace Fabric.Identity.UnitTests
{
    public class FabricCorsPolicyProviderTests
    {
        private readonly List<string> _allowedCorsOrigins;
        private readonly FabricCorsPolicyProvider _fabricCorsPolicyProvider;

        public FabricCorsPolicyProviderTests()
        {
            _allowedCorsOrigins = new List<string>();
            var logger = new Mock<ILogger>();
            var mockPolicyService = new Mock<ICorsPolicyService>();
            mockPolicyService.Setup(policyService => policyService.IsOriginAllowedAsync(It.IsAny<string>()))
                .Returns((string origin) => Task.FromResult(_allowedCorsOrigins.Contains(origin)));

            var serviceCollection = new ServiceCollection();
            serviceCollection.AddSingleton<ICorsPolicyService>(mockPolicyService.Object);
            
            _fabricCorsPolicyProvider = new FabricCorsPolicyProvider(mockPolicyService.Object, logger.Object);
        }

        [Theory, MemberData(nameof(CorsRequestData))]
        public void GetPolicyAsync_ReturnsNull_WrongPolicyName(string allowedOrigin, string corsPolicyName, string requestPath, string requestOrigin)
        {
            _allowedCorsOrigins.Add(allowedOrigin);
            var ctx = new DefaultHttpContext();
            ctx.Request.Path = new PathString(requestPath);
            ctx.Request.Headers.Add("Origin", requestOrigin);
            var policy = _fabricCorsPolicyProvider.GetPolicyAsync(ctx, corsPolicyName).Result;
            Assert.Null(policy);
        }

        [Fact]
        public void GetPolicyAsync_CreatesPolicy_ValidOriginAndPath()
        {
            _allowedCorsOrigins.Add("http://example.com");
            var ctx = new DefaultHttpContext();
            ctx.Request.Path = new PathString("/api/client");
            ctx.Request.Headers.Add("Origin", "http://example.com");
            var policy = _fabricCorsPolicyProvider.GetPolicyAsync(ctx, FabricCorsPolicyProvider.PolicyName).Result;
            Assert.NotNull(policy);
        }

        public static IEnumerable<object[]> CorsRequestData => new[]
        {
            new object[]
            {
                //InvalidPolicyName
                "http://example.com",
                "AnotherCorsPolicy",
                "/api/client",
                "http://example.com"
            },
            new object[]
            {
                //InvalidRequestPath
                "http://example.com",
                "AnotherCorsPolicy",
                "/notavalidpath/getsomedata",
                "http://example.com"
            },
            new object[]
            {
                //Null Origin
                "http://example.com",
                "AnotherCorsPolicy",
                "/api/client",
                null
            },
            new object[]
            {
                //InvalidOrigin
                "http://example.com",
                "AnotherCorsPolicy",
                "/api/client",
                "http://test.com"
            },

        };
    }
}
