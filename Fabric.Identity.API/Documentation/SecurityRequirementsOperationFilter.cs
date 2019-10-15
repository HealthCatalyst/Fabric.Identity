using System.Collections.Generic;
using System.Linq;
using Fabric.Identity.API.Authorization;
using Microsoft.AspNetCore.Authorization;
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
            List<string> policies = new List<string>();

            if (context.ApiDescription.ActionDescriptor is
                Microsoft.AspNetCore.Mvc.Controllers.ControllerActionDescriptor controller)
            {
                policies.AddRange(controller.ControllerTypeInfo
                    .GetCustomAttributes(true)
                    .OfType<AuthorizeAttribute>()
                    .Select(attr => attr.Policy));
            }

            if (context.ApiDescription.TryGetMethodInfo(out var methodInfo))
            {
                policies.AddRange(methodInfo.GetCustomAttributes(true)
                    .OfType<AuthorizeAttribute>()
                    .Select(attr => attr.Policy));
            }

            var requiredClaimTypes = policies
                .Select(x => authorizationOptions.Value.GetPolicy(x))
                .SelectMany(x => x.Requirements)               
                .OfType<IHaveAuthorizationClaimType>()
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
