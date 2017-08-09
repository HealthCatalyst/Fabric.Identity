using System.Linq;
using Swashbuckle.AspNetCore.Swagger;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Fabric.Identity.API.Documentation
{
    /// <summary>
    /// Apply actual version number to API operation paths before generating the documentation.
    /// </summary>
    public class PathVersionDocumentFilter : IDocumentFilter
    {
        /// <summary>
        /// Replace "v{version}" text in action paths with the actual version.
        /// </summary>
        /// <param name="swaggerDoc">SwaggerDocument</param>
        /// <param name="context">DocumentFilterContext</param>
        public void Apply(SwaggerDocument swaggerDoc, DocumentFilterContext context)
        {
            swaggerDoc.Paths = swaggerDoc.Paths
                .ToDictionary(
                    path => path.Key.Replace("v{version}", swaggerDoc.Info.Version),
                    path => path.Value
                );
        }
    }
}
