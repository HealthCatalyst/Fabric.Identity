using System;
using System.Threading.Tasks;
using IdentityServer4.Services;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.AspNetCore.Http;
using Serilog;

namespace Fabric.Identity.API.Infrastructure
{
    public class FabricCorsPolicyProvider : ICorsPolicyProvider
    {
        public static readonly string PolicyName = "FabricCorsPolicy";
        private readonly ICorsPolicyService _corsPolicyService;
        private readonly ILogger _logger;
        private readonly PathString _allowedBasePath = new PathString("/api");
        public FabricCorsPolicyProvider(ICorsPolicyService corsPolicyService, ILogger logger)
        {
            _corsPolicyService = corsPolicyService ?? throw new ArgumentNullException(nameof(corsPolicyService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }
        public async Task<CorsPolicy> GetPolicyAsync(HttpContext context, string policyName)
        {
            if (policyName != PolicyName)
            {
                _logger.Information("PolicyName: {policyName} not applicable to FabricCorsPolicyProvider: {PolicyName}", policyName, PolicyName);
                return null;
            }

            var path = context.Request.Path;
            if (!path.StartsWithSegments(_allowedBasePath))
            {
                _logger.Information("Path: {@path} not allowed - not generating CORS policy.", path);
                return null;
            }
            var origin = context.Request.GetCorsOrigin();
            if (origin == null)
            {
                _logger.Information("No origin specified - not generating a CORS policy.");
                return null;
            }
            
            if (!await _corsPolicyService.IsOriginAllowedAsync(origin))
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
