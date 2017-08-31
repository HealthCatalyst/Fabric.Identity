using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Swashbuckle.AspNetCore.Swagger;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Fabric.Identity.API.Documentation
{
    public class SetBodyParametersRequiredOperationFilter : IOperationFilter
    {
        public void Apply(Operation operation, OperationFilterContext context)
        {
            if (operation.Parameters == null || !operation.Parameters.Any())
            {
                return;
            }

            SetBodyParametersAsRequired(operation);
        }

        private void SetBodyParametersAsRequired(Operation operation)
        {
            IEnumerable<IParameter> bodyParameters = operation.Parameters.Where(p => p.In == "body");

            foreach (IParameter bodyParameter in bodyParameters)
            {
                bodyParameter.Required = true;
            }
        }
    }
}
