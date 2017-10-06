using Swashbuckle.AspNetCore.Swagger;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Fabric.Identity.API.Documentation
{
    public class TagFilter : IDocumentFilter
    {
        public void Apply(SwaggerDocument swaggerDoc, DocumentFilterContext context)
        {
            swaggerDoc.Tags = new[] {
                new Tag { Name = "ApiResource", Description = "Manage ApiResources" },
                new Tag { Name = "Client", Description = "Manage Clients" },
                new Tag { Name = "IdentityResource", Description = "Manage IdentityResources" },
                new Tag { Name = "Users", Description = "Get User Information" },
                new Tag { Name = "IdentityProviders", Description = "Manage IdentityProviders"}
            };
        }
    }
}
