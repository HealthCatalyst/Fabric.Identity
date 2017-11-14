using Fabric.Identity.API.Configuration;
using Fabric.Identity.API.Extensions;
using Fabric.Identity.API.Persistence.SqlServer.Configuration;
using Fabric.Identity.API.Persistence.SqlServer.Services;
using Fabric.Identity.API.Persistence.SqlServer.Stores;
using Fabric.Identity.API.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Serilog;

namespace Fabric.Identity.API.Persistence.SqlServer.DependencyInjection
{
    public class SqlServerIdentityServerConfigurator : BaseIdentityServerConfigurator
    {
        private readonly ICertificateService _certificateService;
        private readonly IConnectionStrings _connectionStrings;
        private readonly SigningCertificateSettings _signingCertificateSettings;

        public SqlServerIdentityServerConfigurator(
            IIdentityServerBuilder identityServerBuilder,
            IServiceCollection serviceCollection,
            ICertificateService certificateService,
            SigningCertificateSettings signingCertificateSettings,
            HostingOptions hostingOptions,
            IConnectionStrings connectionStrings,
            ILogger logger)
            : base(identityServerBuilder, serviceCollection, hostingOptions, logger)
        {
            _certificateService = certificateService;
            _signingCertificateSettings = signingCertificateSettings;
            _connectionStrings = connectionStrings;
        }

        protected override void ConfigureInternalStores()
        {
            ServiceCollection.TryAddSingleton<IConnectionStrings>(_connectionStrings);
            ServiceCollection.AddDbContext<IdentityDbContext>(options => options.UseSqlServer(_connectionStrings.IdentityDatabase));
            ServiceCollection.AddTransient<IIdentityDbContext, IdentityDbContext>();
            ServiceCollection.AddTransient<IApiResourceStore, SqlServerApiResourceStore>();
            ServiceCollection.AddTransient<IIdentityResourceStore, SqlServerIdentityResourceStore>();
            ServiceCollection.AddTransient<IClientManagementStore, SqlServerClientStore>();
            ServiceCollection.AddTransient<IUserStore, SqlServerUserStore>();
            ServiceCollection.AddTransient<IDbBootstrapper, SqlServerBootstrapper>();
            ServiceCollection.AddTransient<IdentityServer4.Stores.IPersistedGrantStore, SqlServerPersistedGrantStore>();
        }

        protected override void ConfigureIdentityServer()
        {
            IdentityServerBuilder?
                .AddSigningCredentialAndValidationKeys(
                    _signingCertificateSettings,
                    _certificateService,
                    Logger)
                .AddTestUsersIfConfigured(HostingOptions)
                .AddCorsPolicyService<CorsPolicyService>()
                .AddResourceStore<SqlServerResourceStore>()
                .AddClientStore<SqlServerClientStore>();
        }
    }
}