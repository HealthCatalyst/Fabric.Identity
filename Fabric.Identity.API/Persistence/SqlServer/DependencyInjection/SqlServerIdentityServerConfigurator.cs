using Fabric.Identity.API.Configuration;
using Fabric.Identity.API.Extensions;
using Fabric.Identity.API.Persistence.SqlServer.Services;
using Fabric.Identity.API.Persistence.SqlServer.Stores;
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
            ServiceCollection.AddSingleton<IIdentityDbContext, IdentityDbContext>();
            ServiceCollection.AddTransient<IApiResourceStore, SqlServerApiResourceStore>();
            ServiceCollection.AddTransient<IIdentityResourceStore, SqlServerIdentityResourceStore>();
            ServiceCollection.AddTransient<IClientManagementStore, SqlServerClientStore>();
            ServiceCollection.AddTransient<IUserStore, SqlServerUserStore>();
            ServiceCollection.AddTransient<IDbBootstrapper, SqlServerBootstrapper>();
            ServiceCollection.AddTransient<IdentityServer4.Stores.IPersistedGrantStore, SqlServerPersistedGrantStore>();
        }

        protected override void ConfigureIdentityServer()
        {
            IdentityServerBuilder
                .AddSigningCredentialAndValidationKeys(
                    AppConfiguration.SigningCertificateSettings,
                    _certificateService,
                    Logger)
                .AddTestUsersIfConfigured(AppConfiguration.HostingOptions)
                .AddCorsPolicyService<CorsPolicyService>()
                .AddResourceStore<SqlServerResourceStore>()
                .AddClientStore<SqlServerClientStore>();
        }
    }
}