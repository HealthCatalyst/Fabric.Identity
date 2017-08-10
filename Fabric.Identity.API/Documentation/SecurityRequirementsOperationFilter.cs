using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fabric.Identity.API.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.Extensions.Options;
using Swashbuckle.AspNetCore.Swagger;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Fabric.Identity.API.Documentation
{
    public class SecurityRequirementsOperationFilter : IOperationFilter
    {
        private readonly IOptions<AuthorizationOptions> authorizationOptions;

        public SecurityRequirementsOperationFilter(IOptions<AuthorizationOptions> authorizationOptions)
        {
            this.authorizationOptions = authorizationOptions;
        }

        public void Apply(Operation operation, OperationFilterContext context)
        {
            var controllerPolicies = context.ApiDescription.ControllerAttributes()
                .OfType<AuthorizeAttribute>()
                .Select(attr => attr.Policy);
            var actionPolicies = context.ApiDescription.ActionAttributes()
                .OfType<AuthorizeAttribute>()
                .Select(attr => attr.Policy);
            var policies = controllerPolicies.Union(actionPolicies).Distinct();
            var requiredClaimTypes = policies
                .Select(x => authorizationOptions.Value.GetPolicy(x))
                .SelectMany(x => x.Requirements)
                .OfType<RegisteredClientThresholdRequirement>()
                .Select(x => x.ClaimType);

            if (requiredClaimTypes.Any())
            {
                operation.Responses.Add("401", new Response { Description = "Unauthorized" });
                operation.Responses.Add("403", new Response { Description = "Forbidden" });

                operation.Security = new List<IDictionary<string, IEnumerable<string>>>();
                operation.Security.Add(
                    new Dictionary<string, IEnumerable<string>>
                    {
                        { "oauth2", requiredClaimTypes }
                    });
            }
        }
    }
}
