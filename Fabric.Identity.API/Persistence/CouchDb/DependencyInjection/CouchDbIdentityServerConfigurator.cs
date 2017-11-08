using Fabric.Identity.API.Configuration;
using Fabric.Identity.API.Extensions;
using Fabric.Identity.API.Persistence.CouchDb.Configuration;
using Fabric.Identity.API.Persistence.CouchDb.Services;
using Fabric.Identity.API.Persistence.CouchDb.Stores;
using Fabric.Identity.API.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Serilog;

namespace Fabric.Identity.API.Persistence.CouchDb.DependencyInjection
{
    public class CouchDbIdentityServerConfigurator : BaseIdentityServerConfigurator
    {
        private readonly ICertificateService _certificateService;

        public CouchDbIdentityServerConfigurator(
            IIdentityServerBuilder identityServerBuilder,
            IServiceCollection serviceCollection,
            ICertificateService certificateService,
            IAppConfiguration appConfiguration,
            ILogger logger) : base(identityServerBuilder, serviceCollection, appConfiguration, logger)
        {
            _certificateService = certificateService;
        }

        protected override void ConfigureInternalStores()
        {
            ServiceCollection.AddSingleton<IDocumentDbService, CouchDbAccessService>();
            ServiceCollection.AddTransient<IApiResourceStore, CouchDbApiResourceStore>();
            ServiceCollection.AddTransient<IIdentityResourceStore, CouchDbIdentityResourceStore>();
            ServiceCollection.AddTransient<IClientManagementStore, CouchDbClientStore>();
            ServiceCollection.AddTransient<IUserStore, CouchDbUserStore>();
            ServiceCollection.AddScopedDecorator<IDocumentDbService, AuditingDocumentDbService>();
            ServiceCollection.AddTransient<IDbBootstrapper, CouchDbBootstrapper>();
            ServiceCollection.AddTransient<IdentityServer4.Stores.IPersistedGrantStore, CouchDbPersistedGrantStore>();
            ServiceCollection.TryAddSingleton<ICouchDbSettings>(AppConfiguration.CouchDbSettings);
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
                .AddResourceStore<CouchDbResourceStore>()
                .AddClientStore<CouchDbClientStore>();
        }
    }
}
