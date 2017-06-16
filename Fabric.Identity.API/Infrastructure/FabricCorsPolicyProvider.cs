using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using IdentityServer4.Services;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace Fabric.Identity.API.Infrastructure
{
    public class FabricCorsPolicyProvider : ICorsPolicyProvider
    {
        private readonly IHttpContextAccessor _httpContext;
        private readonly ILogger _logger;
        public FabricCorsPolicyProvider(IHttpContextAccessor httpContext, ILogger logger)
        {
            _httpContext = httpContext ?? throw new ArgumentNullException(nameof(httpContext));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }
        public async Task<CorsPolicy> GetPolicyAsync(HttpContext context, string policyName)
        {
            var origin = context.Request.GetCorsOrigin();
            if (origin == null)
            {
                _logger.Information("No origin specified - not generating a CORS policy.");
                return null;
            }

            var corsPolicyService = _httpContext.HttpContext.RequestServices.GetRequiredService<ICorsPolicyService>();

            if (!await corsPolicyService.IsOriginAllowedAsync(origin))
            {
                _logger.Information("Origin: {origin} not allowed, not specifying a CORS policy.", origin);
                return null;
            }

            var policyBuilder = new CorsPolicyBuilder()
                .WithOrigins(origin)
                .AllowAnyHeader()
                .AllowAnyMethod();

            var corsPolicy = policyBuilder.Build();
            _logger.Information("Origin: {origin} is a valid origin, generated {@corsPolicy}", origin, corsPolicy);
            return corsPolicy;
        }
    }
}
