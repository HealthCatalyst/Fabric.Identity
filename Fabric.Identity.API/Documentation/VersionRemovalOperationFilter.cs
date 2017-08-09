using System.Linq;
using Swashbuckle.AspNetCore.Swagger;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Fabric.Identity.API.Documentation
{
    /// <summary>
    /// Filter that removes the "version" parameter from the operation's Swagger documentation.
    /// </summary>
    public class VersionRemovalOperationFilter : IOperationFilter
    {
        /// <summary>
        /// Remove the "version" parameter from the <paramref name="operation"/>.
        /// </summary>
        /// <param name="operation">Operation that is being modified</param>
        /// <param name="context">OperationFilterContext</param>
        public void Apply(Operation operation, OperationFilterContext context)
        {
            var versionParameter = operation.Parameters.FirstOrDefault(p => p.Name == "version");

            if (versionParameter == null) return;
            operation.Parameters.Remove(versionParameter);
        }
    }
}
