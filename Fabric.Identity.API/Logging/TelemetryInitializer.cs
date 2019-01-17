using System.Linq;
using Fabric.Identity.API.Configuration;
using IdentityModel;
using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.AspNetCore.Http;

namespace Fabric.Identity.API.Logging
{
    public class TelemetryInitializer : ITelemetryInitializer
    {
        private readonly IAppConfiguration _appConfiguration;
        private readonly IHttpContextAccessor _httpContextAccessor;
        public TelemetryInitializer(IAppConfiguration appConfiguration, IHttpContextAccessor httpContextAccessor)
        {
            _appConfiguration = appConfiguration;
            _httpContextAccessor = httpContextAccessor;
        }
        public void Initialize(ITelemetry telemetry)
        {
            SetGlobalProperties(telemetry);
            SetRequestBasedProperties(telemetry);
        }

        private void SetGlobalProperties(ITelemetry telemetry)
        {
            telemetry.Context.Cloud.RoleName = "Fabric.Identity";
            if (!string.IsNullOrEmpty(_appConfiguration.ClientName))
            {
                telemetry.Context.GlobalProperties["clientName"] = _appConfiguration.ClientName;
            }
            if (!string.IsNullOrEmpty(_appConfiguration.ClientEnvironment))
            {
                telemetry.Context.GlobalProperties["clientEnvironment"] = _appConfiguration.ClientEnvironment;
            }
        }

        private void SetRequestBasedProperties(ITelemetry telemetry)
        {
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext != null)
            {
                if (httpContext.User?.Claims.FirstOrDefault(
                        c => c.Type == FabricIdentityConstants.FabricClaimTypes.FabricId) != null)
                {
                    telemetry.Context.User.Id = httpContext.User?.Claims
                        .FirstOrDefault(c => c.Type == FabricIdentityConstants.FabricClaimTypes.FabricId)
                        ?.Value;
                }
                else
                {
                    telemetry.Context.User.Id = httpContext.User?.Claims
                        .FirstOrDefault(c => c.Type == JwtClaimTypes.Subject)
                        ?.Value;
                }
            }
        }
    }
}
