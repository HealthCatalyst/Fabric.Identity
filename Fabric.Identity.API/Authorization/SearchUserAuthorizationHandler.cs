using Fabric.Identity.API.Configuration;
using Serilog;

namespace Fabric.Identity.API.Authorization
{
    public class SearchUserAuthorizationHandler : BaseAuthorizationHandler<SearchUserScopeRequirement> 
    {
        public SearchUserAuthorizationHandler(IAppConfiguration appConfiguration, ILogger logger) 
            : base(appConfiguration, logger)
        {
            
        }
    }

    
}
