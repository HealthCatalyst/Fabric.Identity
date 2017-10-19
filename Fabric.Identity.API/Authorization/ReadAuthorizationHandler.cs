using Fabric.Identity.API.Configuration;
using Serilog;

namespace Fabric.Identity.API.Authorization
{
    public class ReadAuthorizationHandler : BaseAuthorizationHandler<ReadScopeRequirement>
    {
        public ReadAuthorizationHandler(IAppConfiguration appConfiguration, ILogger logger)
            : base(appConfiguration, logger)
        {
         
        }        
    }
}
