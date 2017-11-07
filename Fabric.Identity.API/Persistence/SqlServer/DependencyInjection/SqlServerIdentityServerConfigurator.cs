using Fabric.Identity.API.Configuration;
using Fabric.Identity.API.Services;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace Fabric.Identity.API.Persistence.SqlServer.DependencyInjection
{
    public class SqlServerIdentityServerConfigurator : BaseIdentityServerConfigurator
    {
        private readonly ICertificateService _certificateService;

        public SqlServerIdentityServerConfigurator(
            IIdentityServerBuilder identityServerBuilder,
            IServiceCollection serviceCollection,
            ICertificateService certificateService,
            IAppConfiguration appConfiguration,
            ILogger logger)
            : base(identityServerBuilder, serviceCollection, appConfiguration, logger)
        {
            _certificateService = certificateService;
        }

        protected override void ConfigureInternalStores()
        {
        }

        protected override void ConfigureIdentityServer()
        {
        }
    }
}